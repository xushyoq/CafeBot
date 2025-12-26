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
    private readonly IProductService _productService;
    private readonly IRoomService _roomService;

    public AdminHandler(ITelegramBotClient botClient, IUserStateManager userStateManager, IEmployeeService employeeService, IProductService productService, IRoomService roomService)
    {
        _botClient = botClient;
        _userStateManager = userStateManager;
        _employeeService = employeeService;
        _productService = productService;
        _roomService = roomService;
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
            case "admin_manage_products":
                await ShowManageProductsMenu(chatId, cancellationToken);
                break;
            case "admin_add_product":
                await StartAddProductFlow(chatId, userId, cancellationToken);
                break;
            case "admin_list_products":
                await ShowProductList(chatId, cancellationToken);
                break;
            case "admin_manage_categories":
                await ShowManageCategoriesMenu(chatId, cancellationToken);
                break;
            case "admin_add_category":
                await StartAddCategoryFlow(chatId, userId, cancellationToken);
                break;
            case "admin_list_categories":
                await ShowCategoryList(chatId, cancellationToken);
                break;
            case "admin_manage_rooms":
                await ShowManageRoomsMenu(chatId, cancellationToken);
                break;
            case "admin_add_room":
                await StartAddRoomFlow(chatId, userId, cancellationToken);
                break;
            case "admin_list_rooms":
                await ShowRoomList(chatId, cancellationToken);
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
                else if (data.StartsWith("set_product_unit_"))
                {
                    await HandleSetProductUnitCallback(userId, chatId, data, callbackQuery.Message.MessageId, cancellationToken);
                }
                else if (data.StartsWith("delete_product_"))
                {
                    await HandleDeleteProductCallback(userId, chatId, data, callbackQuery.Message.MessageId, cancellationToken);
                }
                else if (data.StartsWith("toggle_product_"))
                {
                    await HandleToggleProductCallback(userId, chatId, data, callbackQuery.Message.MessageId, cancellationToken);
                }
                else if (data.StartsWith("delete_category_"))
                {
                    await HandleDeleteCategoryCallback(userId, chatId, data, callbackQuery.Message.MessageId, cancellationToken);
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
            case UserState.AdminAddingCategoryName:
                await HandleCategoryNameInput(chatId, userId, messageText, cancellationToken);
                break;
            case UserState.AdminAddingCategoryDisplayOrder:
                await HandleCategoryDisplayOrderInput(chatId, userId, messageText, cancellationToken);
                break;
            case UserState.AdminAddingProductCategory:
                await HandleProductCategoryInput(chatId, userId, messageText, cancellationToken);
                break;
            case UserState.AdminAddingProductName:
                await HandleProductNameInput(chatId, userId, messageText, cancellationToken);
                break;
            case UserState.AdminAddingProductDescription:
                await HandleProductDescriptionInput(chatId, userId, messageText, cancellationToken);
                break;
            case UserState.AdminAddingProductPrice:
                await HandleProductPriceInput(chatId, userId, messageText, cancellationToken);
                break;
            case UserState.AdminAddingProductPhotoUrl:
                await HandleProductPhotoUrlInput(chatId, userId, messageText, cancellationToken);
                break;
            case UserState.AdminAddingProductDisplayOrder:
                await HandleProductDisplayOrderInput(chatId, userId, messageText, cancellationToken);
                break;
            case UserState.AdminAddingRoomName:
                await HandleRoomNameInput(chatId, userId, messageText, cancellationToken);
                break;
            case UserState.AdminAddingRoomNumber:
                await HandleRoomNumberInput(chatId, userId, messageText, cancellationToken);
                break;
            case UserState.AdminAddingRoomCapacity:
                await HandleRoomCapacityInput(chatId, userId, messageText, cancellationToken);
                break;
            case UserState.AdminAddingRoomDescription:
                await HandleRoomDescriptionInput(chatId, userId, messageText, cancellationToken);
                break;
            case UserState.AdminAddingRoomPhotoUrl:
                await HandleRoomPhotoUrlInput(chatId, userId, messageText, cancellationToken);
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

    // Управление продуктами
    private async Task ShowManageProductsMenu(long chatId, CancellationToken cancellationToken)
    {
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Управление продуктами:",
            replyMarkup: KeyboardBuilder.ManageProductsKeyboard(),
            cancellationToken: cancellationToken);
    }

    private async Task ShowManageCategoriesMenu(long chatId, CancellationToken cancellationToken)
    {
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Управление категориями:",
            replyMarkup: KeyboardBuilder.ManageCategoriesKeyboard(),
            cancellationToken: cancellationToken);
    }

    private async Task StartAddProductFlow(long chatId, long userId, CancellationToken cancellationToken)
    {
        _userStateManager.SetState(userId, UserState.AdminAddingProductCategory);
        _userStateManager.ClearStateData(userId);

        var categories = await _productService.GetActiveCategoriesAsync();
        if (!categories.Any())
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Нет активных категорий. Сначала создайте категорию.",
                replyMarkup: KeyboardBuilder.ManageProductsKeyboard(),
                cancellationToken: cancellationToken);
            _userStateManager.ClearState(userId);
            return;
        }

        var categoryList = "Доступные категории:\n\n";
        foreach (var category in categories)
        {
            categoryList += $"{category.Id}. {category.Name}\n";
        }
        categoryList += "\nВведите ID категории для продукта:";

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: categoryList,
            replyMarkup: new ForceReplyMarkup { Selective = true },
            cancellationToken: cancellationToken);
    }

    private async Task StartAddCategoryFlow(long chatId, long userId, CancellationToken cancellationToken)
    {
        _userStateManager.SetState(userId, UserState.AdminAddingCategoryName);
        _userStateManager.ClearStateData(userId);

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Введите название категории:",
            replyMarkup: new ForceReplyMarkup { Selective = true },
            cancellationToken: cancellationToken);
    }

    private async Task HandleCategoryNameInput(long chatId, long userId, string categoryName, CancellationToken cancellationToken)
    {
        var stateData = _userStateManager.GetStateData(userId);
        stateData.AdminCategoryName = categoryName;

        _userStateManager.SetState(userId, UserState.AdminAddingCategoryDisplayOrder);
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Введите порядок отображения (число, например: 1, 2, 3...):",
            replyMarkup: new ForceReplyMarkup { Selective = true },
            cancellationToken: cancellationToken);
    }

    private async Task HandleCategoryDisplayOrderInput(long chatId, long userId, string displayOrderText, CancellationToken cancellationToken)
    {
        if (!int.TryParse(displayOrderText, out var displayOrder))
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Неверный формат. Введите число для порядка отображения:",
                cancellationToken: cancellationToken);
            return;
        }

        var stateData = _userStateManager.GetStateData(userId);
        if (string.IsNullOrEmpty(stateData.AdminCategoryName))
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Ошибка: название категории не найдено. Начните заново.",
                replyMarkup: KeyboardBuilder.ManageCategoriesKeyboard(),
                cancellationToken: cancellationToken);
            _userStateManager.ClearState(userId);
            return;
        }

        try
        {
            var category = await _productService.CreateCategoryAsync(stateData.AdminCategoryName, displayOrder);

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"✅ Категория '{category.Name}' успешно создана!",
                replyMarkup: KeyboardBuilder.ManageCategoriesKeyboard(),
                cancellationToken: cancellationToken);

            _userStateManager.ClearState(userId);
        }
        catch (Exception ex)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"❌ Ошибка при создании категории: {ex.Message}",
                replyMarkup: KeyboardBuilder.ManageCategoriesKeyboard(),
                cancellationToken: cancellationToken);
            _userStateManager.ClearState(userId);
        }
    }

    private async Task HandleProductCategoryInput(long chatId, long userId, string categoryIdText, CancellationToken cancellationToken)
    {
        if (!int.TryParse(categoryIdText, out var categoryId))
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Неверный формат. Введите ID категории (число):",
                cancellationToken: cancellationToken);
            return;
        }

        var category = await _productService.GetCategoryByIdAsync(categoryId);
        if (category == null)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Категория с таким ID не найдена. Попробуйте еще раз:",
                cancellationToken: cancellationToken);
            return;
        }

        var stateData = _userStateManager.GetStateData(userId);
        stateData.AdminProductCategoryId = categoryId;

        _userStateManager.SetState(userId, UserState.AdminAddingProductName);
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Введите название продукта:",
            replyMarkup: new ForceReplyMarkup { Selective = true },
            cancellationToken: cancellationToken);
    }

    private async Task HandleProductNameInput(long chatId, long userId, string productName, CancellationToken cancellationToken)
    {
        var stateData = _userStateManager.GetStateData(userId);
        stateData.AdminProductName = productName;

        _userStateManager.SetState(userId, UserState.AdminAddingProductDescription);
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Введите описание продукта (или '-' для пропуска):",
            replyMarkup: new ForceReplyMarkup { Selective = true },
            cancellationToken: cancellationToken);
    }

    private async Task HandleProductDescriptionInput(long chatId, long userId, string description, CancellationToken cancellationToken)
    {
        var stateData = _userStateManager.GetStateData(userId);
        stateData.AdminProductDescription = description == "-" ? null : description;

        _userStateManager.SetState(userId, UserState.AdminAddingProductPrice);
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Введите цену продукта (число, например: 15000, 25000):",
            replyMarkup: new ForceReplyMarkup { Selective = true },
            cancellationToken: cancellationToken);
    }

    private async Task HandleProductPriceInput(long chatId, long userId, string priceText, CancellationToken cancellationToken)
    {
        if (!decimal.TryParse(priceText, out var price) || price <= 0)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Неверный формат цены. Введите положительное число:",
                cancellationToken: cancellationToken);
            return;
        }

        var stateData = _userStateManager.GetStateData(userId);
        stateData.AdminProductPrice = price;

        _userStateManager.SetState(userId, UserState.AdminAddingProductUnit);
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Выберите единицу измерения:",
            replyMarkup: KeyboardBuilder.ProductUnitKeyboard(),
            cancellationToken: cancellationToken);
    }

    private async Task HandleSetProductUnitCallback(long userId, long chatId, string callbackData, int messageId, CancellationToken cancellationToken)
    {
        var parts = callbackData.Split('_');
        if (parts.Length == 4 && Enum.TryParse<ProductUnit>(parts[3], out var unit))
        {
            var stateData = _userStateManager.GetStateData(userId);
            stateData.AdminProductUnit = unit;

            _userStateManager.SetState(userId, UserState.AdminAddingProductPhotoUrl);
            await _botClient.EditMessageTextAsync(
                chatId: chatId,
                messageId: messageId,
                text: "Введите URL фото продукта (или '-' для пропуска):",
                cancellationToken: cancellationToken);

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Введите URL фото продукта (или '-' для пропуска):",
                replyMarkup: new ForceReplyMarkup { Selective = true },
                cancellationToken: cancellationToken);
        }
    }

    private async Task HandleProductPhotoUrlInput(long chatId, long userId, string photoUrl, CancellationToken cancellationToken)
    {
        var stateData = _userStateManager.GetStateData(userId);
        stateData.AdminProductPhotoUrl = photoUrl == "-" ? null : photoUrl;

        _userStateManager.SetState(userId, UserState.AdminAddingProductDisplayOrder);
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Введите порядок отображения (число, например: 1, 2, 3...):",
            replyMarkup: new ForceReplyMarkup { Selective = true },
            cancellationToken: cancellationToken);
    }

    private async Task HandleProductDisplayOrderInput(long chatId, long userId, string displayOrderText, CancellationToken cancellationToken)
    {
        if (!int.TryParse(displayOrderText, out var displayOrder))
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Неверный формат. Введите число для порядка отображения:",
                cancellationToken: cancellationToken);
            return;
        }

        var stateData = _userStateManager.GetStateData(userId);

        // Проверяем, что все обязательные поля заполнены
        if (!stateData.AdminProductCategoryId.HasValue ||
            string.IsNullOrEmpty(stateData.AdminProductName) ||
            !stateData.AdminProductPrice.HasValue ||
            !stateData.AdminProductUnit.HasValue)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Ошибка: не все данные заполнены. Начните заново.",
                replyMarkup: KeyboardBuilder.ManageProductsKeyboard(),
                cancellationToken: cancellationToken);
            _userStateManager.ClearState(userId);
            return;
        }

        try
        {
            var product = await _productService.CreateProductAsync(
                stateData.AdminProductCategoryId.Value,
                stateData.AdminProductName,
                stateData.AdminProductDescription,
                stateData.AdminProductPrice.Value,
                stateData.AdminProductUnit.Value,
                stateData.AdminProductPhotoUrl,
                displayOrder);

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"✅ Продукт '{product.Name}' успешно создан!",
                replyMarkup: KeyboardBuilder.ManageProductsKeyboard(),
                cancellationToken: cancellationToken);

            _userStateManager.ClearState(userId);
        }
        catch (Exception ex)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"❌ Ошибка при создании продукта: {ex.Message}",
                replyMarkup: KeyboardBuilder.ManageProductsKeyboard(),
                cancellationToken: cancellationToken);
            _userStateManager.ClearState(userId);
        }
    }

    private async Task ShowProductList(long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var products = await _productService.GetAvailableProductsAsync();
            var categories = await _productService.GetActiveCategoriesAsync();

            if (!products.Any())
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "📦 В данный момент нет доступных продуктов.",
                    replyMarkup: KeyboardBuilder.ManageProductsKeyboard(),
                    cancellationToken: cancellationToken);
                return;
            }

            var categoryDict = categories.ToDictionary(c => c.Id, c => c.Name);
            var message = "📦 Список продуктов:\n\n";

            foreach (var product in products.OrderBy(p => p.CategoryId).ThenBy(p => p.DisplayOrder))
            {
                var categoryName = categoryDict.ContainsKey(product.CategoryId) ? categoryDict[product.CategoryId] : "Неизвестная категория";
                message += $"🛒 {product.Name}\n";
                message += $"   Категория: {categoryName}\n";
                message += $"   Цена: {product.Price:N0} сум\n";
                message += $"   Единица: {product.Unit}\n";
                if (!string.IsNullOrEmpty(product.Description))
                    message += $"   Описание: {product.Description}\n";
                message += $"   Доступен: {(product.IsAvailable ? "✅ Да" : "❌ Нет")}\n\n";
            }

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: message.Trim(),
                replyMarkup: KeyboardBuilder.ManageProductsKeyboard(),
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"❌ Ошибка при получении списка продуктов: {ex.Message}",
                replyMarkup: KeyboardBuilder.ManageProductsKeyboard(),
                cancellationToken: cancellationToken);
        }
    }

    private async Task ShowCategoryList(long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var categories = await _productService.GetActiveCategoriesAsync();

            if (!categories.Any())
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "📁 В данный момент нет активных категорий.",
                    replyMarkup: KeyboardBuilder.ManageCategoriesKeyboard(),
                    cancellationToken: cancellationToken);
                return;
            }

            var message = "📁 Список категорий:\n\n";
            foreach (var category in categories.OrderBy(c => c.DisplayOrder))
            {
                message += $"📂 {category.Name}\n";
                message += $"   ID: {category.Id}\n";
                message += $"   Порядок: {category.DisplayOrder}\n";
                message += $"   Активна: {(category.IsActive ? "✅ Да" : "❌ Нет")}\n\n";
            }

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: message.Trim(),
                replyMarkup: KeyboardBuilder.ManageCategoriesKeyboard(),
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"❌ Ошибка при получении списка категорий: {ex.Message}",
                replyMarkup: KeyboardBuilder.ManageCategoriesKeyboard(),
                cancellationToken: cancellationToken);
        }
    }

    private async Task HandleDeleteProductCallback(long userId, long chatId, string callbackData, int messageId, CancellationToken cancellationToken)
    {
        // Заглушка для будущей реализации удаления продуктов
        await _botClient.AnswerCallbackQueryAsync(callbackData, "Функция удаления продуктов будет реализована позже", cancellationToken: cancellationToken);
    }

    private async Task HandleToggleProductCallback(long userId, long chatId, string callbackData, int messageId, CancellationToken cancellationToken)
    {
        // Заглушка для будущей реализации переключения доступности продуктов
        await _botClient.AnswerCallbackQueryAsync(callbackData, "Функция переключения доступности будет реализована позже", cancellationToken: cancellationToken);
    }

    private async Task HandleDeleteCategoryCallback(long userId, long chatId, string callbackData, int messageId, CancellationToken cancellationToken)
    {
        // Заглушка для будущей реализации удаления категорий
        await _botClient.AnswerCallbackQueryAsync(callbackData, "Функция удаления категорий будет реализована позже", cancellationToken: cancellationToken);
    }

    // Управление комнатами
    private async Task ShowManageRoomsMenu(long chatId, CancellationToken cancellationToken)
    {
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Управление комнатами:",
            replyMarkup: KeyboardBuilder.ManageRoomsKeyboard(),
            cancellationToken: cancellationToken);
    }

    private async Task StartAddRoomFlow(long chatId, long userId, CancellationToken cancellationToken)
    {
        _userStateManager.SetState(userId, UserState.AdminAddingRoomName);
        _userStateManager.ClearStateData(userId);

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Введите название комнаты:",
            replyMarkup: new ForceReplyMarkup { Selective = true },
            cancellationToken: cancellationToken);
    }

    private async Task HandleRoomNameInput(long chatId, long userId, string roomName, CancellationToken cancellationToken)
    {
        var stateData = _userStateManager.GetStateData(userId);
        stateData.AdminRoomName = roomName;

        _userStateManager.SetState(userId, UserState.AdminAddingRoomNumber);
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Введите номер комнаты (число, или '-' для пропуска):",
            replyMarkup: new ForceReplyMarkup { Selective = true },
            cancellationToken: cancellationToken);
    }

    private async Task HandleRoomNumberInput(long chatId, long userId, string roomNumberText, CancellationToken cancellationToken)
    {
        var stateData = _userStateManager.GetStateData(userId);

        if (roomNumberText == "-")
        {
            stateData.AdminRoomNumber = null;
        }
        else if (int.TryParse(roomNumberText, out var roomNumber) && roomNumber > 0)
        {
            stateData.AdminRoomNumber = roomNumber;
        }
        else
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Неверный формат номера. Введите положительное число или '-' для пропуска:",
                cancellationToken: cancellationToken);
            return;
        }

        _userStateManager.SetState(userId, UserState.AdminAddingRoomCapacity);
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Введите вместимость комнаты (число человек):",
            replyMarkup: new ForceReplyMarkup { Selective = true },
            cancellationToken: cancellationToken);
    }

    private async Task HandleRoomCapacityInput(long chatId, long userId, string capacityText, CancellationToken cancellationToken)
    {
        if (!int.TryParse(capacityText, out var capacity) || capacity <= 0)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Неверный формат вместимости. Введите положительное число:",
                cancellationToken: cancellationToken);
            return;
        }

        var stateData = _userStateManager.GetStateData(userId);
        stateData.AdminRoomCapacity = capacity;

        _userStateManager.SetState(userId, UserState.AdminAddingRoomDescription);
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Введите описание комнаты (или '-' для пропуска):",
            replyMarkup: new ForceReplyMarkup { Selective = true },
            cancellationToken: cancellationToken);
    }

    private async Task HandleRoomDescriptionInput(long chatId, long userId, string description, CancellationToken cancellationToken)
    {
        var stateData = _userStateManager.GetStateData(userId);
        stateData.AdminRoomDescription = description == "-" ? null : description;

        _userStateManager.SetState(userId, UserState.AdminAddingRoomPhotoUrl);
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Введите URL фото комнаты (или '-' для пропуска):",
            replyMarkup: new ForceReplyMarkup { Selective = true },
            cancellationToken: cancellationToken);
    }

    private async Task HandleRoomPhotoUrlInput(long chatId, long userId, string photoUrl, CancellationToken cancellationToken)
    {
        var stateData = _userStateManager.GetStateData(userId);

        // Проверяем, что все обязательные поля заполнены
        if (string.IsNullOrEmpty(stateData.AdminRoomName) || !stateData.AdminRoomCapacity.HasValue)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Ошибка: не все данные заполнены. Начните заново.",
                replyMarkup: KeyboardBuilder.ManageRoomsKeyboard(),
                cancellationToken: cancellationToken);
            _userStateManager.ClearState(userId);
            return;
        }

        try
        {
            var room = await _roomService.CreateRoomAsync(
                stateData.AdminRoomName,
                stateData.AdminRoomNumber,
                stateData.AdminRoomCapacity.Value,
                stateData.AdminRoomDescription,
                photoUrl == "-" ? null : photoUrl);

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"✅ Комната '{room.Name}' успешно создана!",
                replyMarkup: KeyboardBuilder.ManageRoomsKeyboard(),
                cancellationToken: cancellationToken);

            _userStateManager.ClearState(userId);
        }
        catch (Exception ex)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"❌ Ошибка при создании комнаты: {ex.Message}",
                replyMarkup: KeyboardBuilder.ManageRoomsKeyboard(),
                cancellationToken: cancellationToken);
            _userStateManager.ClearState(userId);
        }
    }

    private async Task ShowRoomList(long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var rooms = await _roomService.GetActiveRoomsAsync();

            if (!rooms.Any())
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "🏠 В данный момент нет активных комнат.",
                    replyMarkup: KeyboardBuilder.ManageRoomsKeyboard(),
                    cancellationToken: cancellationToken);
                return;
            }

            var message = "🏠 Список комнат:\n\n";
            foreach (var room in rooms.OrderBy(r => r.Number ?? 999).ThenBy(r => r.Name))
            {
                message += $"🏠 {room.Name}";
                if (room.Number.HasValue)
                    message += $" (№{room.Number})";
                message += $"\n";
                message += $"   Вместимость: {room.Capacity} чел.\n";
                message += $"   Статус: {(room.Status == RoomStatus.Active ? "✅ Активна" : "❌ Неактивна")}\n";
                if (!string.IsNullOrEmpty(room.Description))
                    message += $"   Описание: {room.Description}\n";
                message += "\n";
            }

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: message.Trim(),
                replyMarkup: KeyboardBuilder.ManageRoomsKeyboard(),
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"❌ Ошибка при получении списка комнат: {ex.Message}",
                replyMarkup: KeyboardBuilder.ManageRoomsKeyboard(),
                cancellationToken: cancellationToken);
        }
    }
}