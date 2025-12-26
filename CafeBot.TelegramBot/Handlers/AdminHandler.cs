using CafeBot.Application.Services;
using CafeBot.Core.Enums;
using CafeBot.TelegramBot.Keyboards;
using CafeBot.TelegramBot.States;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups; 

namespace CafeBot.TelegramBot.Handlers;

public class AdminHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly IUserStateManager _userStateManager;
    private readonly IEmployeeService _employeeService;

    public AdminHandler(ITelegramBotClient botClient, IUserStateManager userStateManager, IEmployeeService employeeService)
    {
        _botClient = botClient;
        _userStateManager = userStateManager;
        _employeeService = employeeService;
    }

    public async Task HandleAdminPanelCommand(Message message, CancellationToken cancellationToken)
    {
        // Clear any previous admin-related state
        _userStateManager.ClearState(message.From!.Id);
        await ShowAdminMainMenu(message.Chat.Id, cancellationToken);
    }

    private async Task ShowAdminMainMenu(long chatId, CancellationToken cancellationToken)
    {
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Добро пожаловать в админ-панель!",
            replyMarkup: KeyboardBuilder.AdminMainMenuKeyboard(), 
            cancellationToken: cancellationToken);
    }

    public async Task HandleAdminCallbackQuery(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var userId = callbackQuery.From!.Id;
        var chatId = callbackQuery.Message!.Chat.Id;
        var data = callbackQuery.Data ?? string.Empty;

        switch (data)
        {
            case "admin_manage_employees":
                await ShowManageEmployeesMenu(chatId, cancellationToken);
                break;
            case "admin_add_employee":
                await StartAddEmployeeFlow(chatId, userId, cancellationToken);
                break;
            case "admin_list_employees":
                await ShowEmployeeList(chatId, cancellationToken);
                break;
            case "admin_employee_statistics":
                await ShowStatisticsPeriodSelection(chatId, cancellationToken);
                break;
            case "admin_employee_status":
                await ShowEmployeeCurrentStatus(chatId, cancellationToken);
                break;
            case "admin_back_to_main":
                await _botClient.EditMessageTextAsync(
                    chatId: chatId,
                    messageId: callbackQuery.Message.MessageId,
                    text: "Вы вернулись в главное меню.",
                    replyMarkup: null, // Изменено на null, так как MainMenuKeyboard не является InlineKeyboardMarkup
                    cancellationToken: cancellationToken);
                break;
            case "admin_back_to_admin_menu":
                await _botClient.EditMessageTextAsync(
                    chatId: chatId,
                    messageId: callbackQuery.Message.MessageId,
                    text: "Вы вернулись в админ-панель.",
                    replyMarkup: KeyboardBuilder.AdminMainMenuKeyboard(), // Убрано явное приведение
                    cancellationToken: cancellationToken);
                break;
            case "admin_cancel":
                _userStateManager.ClearState(userId);
                await _botClient.EditMessageTextAsync(
                    chatId: chatId,
                    messageId: callbackQuery.Message.MessageId,
                    text: "Операция отменена. Вы вернулись в главное меню.",
                    replyMarkup: null, // Изменено на null, так как MainMenuKeyboard не является InlineKeyboardMarkup
                    cancellationToken: cancellationToken);
                break;
            default:
                if (data.StartsWith("set_employee_role_"))
                {
                    await HandleSetEmployeeRoleCallback(userId, chatId, data, callbackQuery.Message.MessageId, cancellationToken);
                }
                else if (data.StartsWith("stats_period_"))
                {
                    await HandleStatisticsPeriodCallback(userId, chatId, data, cancellationToken);
                }
                break;
        }

        await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
    }

    public async Task HandleAdminTextMessageAsync(Message message, CancellationToken cancellationToken)
    {
        var userId = message.From!.Id;
        var chatId = message.Chat.Id;
        var messageText = message.Text ?? string.Empty;

        var currentState = _userStateManager.GetState(userId);
        var stateData = _userStateManager.GetStateData(userId);

        switch (currentState)
        {
            case UserState.AdminAddingEmployeeTelegramId:
                if (long.TryParse(messageText, out var telegramId))
                {
                    stateData.AdminEmployeeTelegramId = telegramId;
                    _userStateManager.SetState(userId, UserState.AdminAddingEmployeeFirstName);
                    await RequestEmployeeFirstName(chatId, cancellationToken);
                }
                else
                {
                    await _botClient.SendTextMessageAsync(chatId, "Пожалуйста, введите корректный Telegram ID (только цифры).");
                }
                break;
            case UserState.AdminAddingEmployeeFirstName:
                stateData.AdminEmployeeFirstName = messageText;
                _userStateManager.SetState(userId, UserState.AdminAddingEmployeeLastName);
                await RequestEmployeeLastName(chatId, cancellationToken);
                break;
            case UserState.AdminAddingEmployeeLastName:
                stateData.AdminEmployeeLastName = messageText;
                _userStateManager.SetState(userId, UserState.AdminAddingEmployeePhone);
                await RequestEmployeePhone(chatId, cancellationToken);
                break;
            case UserState.AdminAddingEmployeePhone:
                stateData.AdminEmployeePhone = messageText;
                _userStateManager.SetState(userId, UserState.AdminSelectingEmployeeRole);
                await RequestEmployeeRole(chatId, cancellationToken);
                break;
            case UserState.AdminSelectingStatisticsStartDate:
                await HandleStatisticsStartDateInput(chatId, userId, messageText, cancellationToken);
                break;
            case UserState.AdminSelectingStatisticsEndDate:
                await HandleStatisticsEndDateInput(chatId, userId, messageText, cancellationToken);
                break;
            default:
                await _botClient.SendTextMessageAsync(chatId, "Неизвестная команда. Пожалуйста, используйте кнопки.", replyMarkup: KeyboardBuilder.AdminMainMenuKeyboard(), cancellationToken: cancellationToken);
                break;
        }
    }

    private async Task ShowManageEmployeesMenu(long chatId, CancellationToken cancellationToken)
    {
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Управление сотрудниками:",
            replyMarkup: KeyboardBuilder.ManageEmployeesKeyboard(), 
            cancellationToken: cancellationToken);
    }

    private async Task StartAddEmployeeFlow(long chatId, long userId, CancellationToken cancellationToken)
    {
        _userStateManager.SetState(userId, UserState.AdminAddingEmployeeTelegramId);
        _userStateManager.ClearStateData(userId); // Clear previous data for new employee
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Введите Telegram ID нового сотрудника (только цифры):",
            replyMarkup: new ForceReplyMarkup { Selective = true },
            cancellationToken: cancellationToken);
    }

    private async Task RequestEmployeeFirstName(long chatId, CancellationToken cancellationToken)
    {
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Введите имя сотрудника:",
            replyMarkup: new ForceReplyMarkup { Selective = true },
            cancellationToken: cancellationToken);
    }

    private async Task RequestEmployeeLastName(long chatId, CancellationToken cancellationToken)
    {
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Введите фамилию сотрудника:",
            replyMarkup: new ForceReplyMarkup { Selective = true },
            cancellationToken: cancellationToken);
    }

    private async Task RequestEmployeePhone(long chatId, CancellationToken cancellationToken)
    {
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Введите номер телефона сотрудника (например, +79123456789):",
            replyMarkup: new ForceReplyMarkup { Selective = true },
            cancellationToken: cancellationToken);
    }

    private async Task RequestEmployeeRole(long chatId, CancellationToken cancellationToken)
    {
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Выберите роль для сотрудника:",
            replyMarkup: KeyboardBuilder.EmployeeRolesKeyboard(), 
            cancellationToken: cancellationToken);
    }

    private async Task HandleSetEmployeeRoleCallback(long userId, long chatId, string callbackData, int messageId, CancellationToken cancellationToken)
    {
        var parts = callbackData.Split('_');
        if (parts.Length == 4 && Enum.TryParse<EmployeeRole>(parts[3], out var role))
        {
            var stateData = _userStateManager.GetStateData(userId);
            stateData.AdminEmployeeRole = role;

            // Confirm and create employee
            await ConfirmAddEmployee(userId, chatId, messageId, cancellationToken);
        }
    }

    private async Task ConfirmAddEmployee(long userId, long chatId, int messageId, CancellationToken cancellationToken)
    {
        var stateData = _userStateManager.GetStateData(userId);

        if (stateData.AdminEmployeeTelegramId.HasValue &&
            !string.IsNullOrEmpty(stateData.AdminEmployeeFirstName) &&
            !string.IsNullOrEmpty(stateData.AdminEmployeeLastName) &&
            !string.IsNullOrEmpty(stateData.AdminEmployeePhone) &&
            stateData.AdminEmployeeRole.HasValue)
        {
            var existingEmployee = await _employeeService.GetEmployeeByTelegramIdAsync(stateData.AdminEmployeeTelegramId.Value);
            if (existingEmployee != null)
            {
                await _botClient.EditMessageTextAsync(
                    chatId: chatId,
                    messageId: messageId,
                    text: $"❌ Сотрудник с Telegram ID {stateData.AdminEmployeeTelegramId.Value} уже существует.",
                    replyMarkup: KeyboardBuilder.ManageEmployeesKeyboard(), // Убрано явное приведение
                    cancellationToken: cancellationToken);
                _userStateManager.ClearState(userId);
                return;
            }

            var newEmployee = await _employeeService.CreateEmployeeAsync(
                stateData.AdminEmployeeTelegramId.Value,
                stateData.AdminEmployeeFirstName,
                stateData.AdminEmployeeLastName,
                stateData.AdminEmployeePhone,
                stateData.AdminEmployeeRole.Value
            );

            await _botClient.EditMessageTextAsync(
                chatId: chatId,
                messageId: messageId,
                text: $"✅ Сотрудник {newEmployee.FirstName} {newEmployee.LastName} ({newEmployee.Role}) успешно добавлен!",
                replyMarkup: KeyboardBuilder.ManageEmployeesKeyboard(), // Убрано явное приведение
                cancellationToken: cancellationToken);
            
            _userStateManager.ClearState(userId);
        }
        else
        {
            await _botClient.EditMessageTextAsync(
                chatId: chatId,
                messageId: messageId,
                text: "Ошибка при создании сотрудника. Не все данные заполнены.",
                replyMarkup: KeyboardBuilder.ManageEmployeesKeyboard(), // Убрано явное приведение
                cancellationToken: cancellationToken);
            _userStateManager.ClearState(userId);
        }
    }

    private async Task ShowEmployeeList(long chatId, CancellationToken cancellationToken)
    {
        var employees = await _employeeService.GetAllEmployeesAsync();

        if (!employees.Any())
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "В данный момент нет зарегистрированных сотрудников.",
                replyMarkup: KeyboardBuilder.ManageEmployeesKeyboard(), 
                cancellationToken: cancellationToken);
            return;
        }

        var employeeList = new System.Text.StringBuilder();
        employeeList.AppendLine("Список сотрудников:");
        employeeList.AppendLine();

        foreach (var employee in employees)
        {
            employeeList.AppendLine($"👨‍💼 ID: {employee.Id}, Telegram ID: {employee.TelegramId}");
            employeeList.AppendLine($"  Имя: {employee.FirstName} {employee.LastName}");
            employeeList.AppendLine($"  Телефон: {employee.Phone}");
            employeeList.AppendLine($"  Роль: {employee.Role}");
            employeeList.AppendLine($"  Активен: {(employee.IsActive ? "✅ Да" : "❌ Нет")}");
            employeeList.AppendLine();
        }

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: employeeList.ToString(),
            parseMode: ParseMode.Html,
            replyMarkup: KeyboardBuilder.ManageEmployeesKeyboard(),
            cancellationToken: cancellationToken);
    }

    private async Task ShowStatisticsPeriodSelection(long chatId, CancellationToken cancellationToken)
    {
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "📊 Выберите период для просмотра статистики сотрудников:",
            replyMarkup: KeyboardBuilder.StatisticsPeriodKeyboard(),
            cancellationToken: cancellationToken);
    }

    private async Task HandleStatisticsPeriodCallback(long userId, long chatId, string callbackData, CancellationToken cancellationToken)
    {
        DateTime startDate, endDate;

        switch (callbackData)
        {
            case "stats_period_today":
                startDate = DateTime.Today.ToUniversalTime();
                endDate = DateTime.Today.AddDays(1).ToUniversalTime();
                break;
            case "stats_period_week":
                startDate = DateTime.Today.AddDays(-7).ToUniversalTime();
                endDate = DateTime.Today.AddDays(1).ToUniversalTime();
                break;
            case "stats_period_month":
                startDate = DateTime.Today.AddDays(-30).ToUniversalTime();
                endDate = DateTime.Today.AddDays(1).ToUniversalTime();
                break;
            case "stats_period_custom":
                await StartCustomPeriodSelection(chatId, userId, cancellationToken);
                return;
            default:
                return;
        }

        await ShowEmployeeStatistics(chatId, startDate, endDate, cancellationToken);
    }

    private async Task StartCustomPeriodSelection(long chatId, long userId, CancellationToken cancellationToken)
    {
        _userStateManager.SetState(userId, UserState.AdminSelectingStatisticsStartDate);
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "📅 Введите дату НАЧАЛА периода в формате ДД.ММ.ГГГГ (например: 01.12.2025):",
            cancellationToken: cancellationToken);
    }

    private async Task HandleStatisticsStartDateInput(long chatId, long userId, string dateText, CancellationToken cancellationToken)
    {
        if (!DateTime.TryParseExact(dateText, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.AssumeLocal, out var startDate))
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Неверный формат даты. Введите дату в формате ДД.ММ.ГГГГ (например: 01.12.2025):",
                cancellationToken: cancellationToken);
            return;
        }

        var stateData = _userStateManager.GetStateData(userId);
        stateData.AdminStatisticsStartDate = startDate.ToUniversalTime(); // Конвертация в UTC

        _userStateManager.SetState(userId, UserState.AdminSelectingStatisticsEndDate);
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: $"✅ Дата начала: {startDate:dd.MM.yyyy}\n\n📅 Теперь введите дату КОНЦА периода в формате ДД.ММ.ГГГГ:",
            cancellationToken: cancellationToken);
    }

    private async Task HandleStatisticsEndDateInput(long chatId, long userId, string dateText, CancellationToken cancellationToken)
    {
        if (!DateTime.TryParseExact(dateText, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.AssumeLocal, out var endDate))
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Неверный формат даты. Введите дату в формате ДД.ММ.ГГГГ (например: 31.12.2025):",
                cancellationToken: cancellationToken);
            return;
        }

        var stateData = _userStateManager.GetStateData(userId);
        if (!stateData.AdminStatisticsStartDate.HasValue)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Ошибка: дата начала не найдена. Начните заново.",
                replyMarkup: KeyboardBuilder.AdminMainMenuKeyboard(),
                cancellationToken: cancellationToken);
            _userStateManager.ClearState(userId);
            return;
        }

        var startDateUtc = stateData.AdminStatisticsStartDate.Value;
        var startDateLocal = startDateUtc.ToLocalTime().Date; // Только дата без времени для корректного сравнения

        if (endDate.Date <= startDateLocal)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Дата конца должна быть позже даты начала. Попробуйте еще раз:",
                cancellationToken: cancellationToken);
            return;
        }

        _userStateManager.ClearState(userId);
        await ShowEmployeeStatistics(chatId, startDateUtc, endDate.ToUniversalTime(), cancellationToken); // Конвертация в UTC
    }

    private async Task ShowEmployeeStatistics(long chatId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        try
        {
            var statistics = await _employeeService.GetEmployeesStatisticsAsync(startDate, endDate);

            if (!statistics.Any())
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"📊 Статистика за период {startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}\n\n❌ За выбранный период нет завершенных заказов.",
                    replyMarkup: KeyboardBuilder.AdminMainMenuKeyboard(),
                    cancellationToken: cancellationToken);
                return;
            }

            var message = $"📊 Статистика сотрудников\n📅 Период: {startDate.ToLocalTime():dd.MM.yyyy} - {endDate.ToLocalTime():dd.MM.yyyy}\n\n";

            var sortedStats = statistics.OrderByDescending(s => s.TotalRevenue).ToList();

            for (int i = 0; i < sortedStats.Count; i++)
            {
                var stat = sortedStats[i];
                var medal = i switch
                {
                    0 => "🥇",
                    1 => "🥈",
                    2 => "🥉",
                    _ => "👤"
                };

                message += $"{medal} {stat.EmployeeName}\n";
                message += $"   📋 Заказов: {stat.OrdersCount}\n";
                message += $"   💰 Выручка: {stat.TotalRevenue:N0} сум\n\n";
            }

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: message.Trim(),
                replyMarkup: KeyboardBuilder.AdminMainMenuKeyboard(),
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"❌ Ошибка при получении статистики: {ex.Message}",
                replyMarkup: KeyboardBuilder.AdminMainMenuKeyboard(),
                cancellationToken: cancellationToken);
        }
    }

    private async Task ShowEmployeeCurrentStatus(long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var statusList = await _employeeService.GetEmployeesCurrentStatusAsync();

            if (!statusList.Any())
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "👀 Статус официантов\n\n❌ Нет активных официантов.",
                    replyMarkup: KeyboardBuilder.AdminMainMenuKeyboard(),
                    cancellationToken: cancellationToken);
                return;
            }

            var message = "👀 Статус официантов\n\n";

            foreach (var status in statusList.OrderBy(s => s.EmployeeName))
            {
                message += $"👨‍💼 {status.EmployeeName}\n";

                if (status.Status == "Свободен")
                {
                    message += $"   ✅ Свободен\n\n";
                }
                else
                {
                    message += $"   🔄 {status.Status}\n\n";
                }
            }

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: message.Trim(),
                replyMarkup: KeyboardBuilder.AdminMainMenuKeyboard(),
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"❌ Ошибка при получении статуса: {ex.Message}",
                replyMarkup: KeyboardBuilder.AdminMainMenuKeyboard(),
                cancellationToken: cancellationToken);
        }
    }
}