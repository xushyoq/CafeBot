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
}