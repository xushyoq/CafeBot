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
    private readonly EmployeeAdminHandler _employeeAdminHandler;
    private readonly ProductAdminHandler _productAdminHandler;
    private readonly CategoryAdminHandler _categoryAdminHandler;
    private readonly RoomAdminHandler _roomAdminHandler;

    public AdminHandler(
        ITelegramBotClient botClient,
        IUserStateManager userStateManager,
        EmployeeAdminHandler employeeAdminHandler,
        ProductAdminHandler productAdminHandler,
        CategoryAdminHandler categoryAdminHandler,
        RoomAdminHandler roomAdminHandler)
    {
        _botClient = botClient;
        _userStateManager = userStateManager;
        _employeeAdminHandler = employeeAdminHandler;
        _productAdminHandler = productAdminHandler;
        _categoryAdminHandler = categoryAdminHandler;
        _roomAdminHandler = roomAdminHandler;
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
            text: "Xush kelibsiz в админ-панель!",
            replyMarkup: KeyboardBuilder.AdminMainMenuKeyboard(),
            cancellationToken: cancellationToken);
    }

    public async Task HandleAdminCallbackQuery(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var userId = callbackQuery.From!.Id;
        var chatId = callbackQuery.Message!.Chat.Id;
        var data = callbackQuery.Data ?? string.Empty;

        // Маршрутизация к соответствующим handler'ам
        if (data.StartsWith("admin_manage_employees") || data.StartsWith("admin_add_employee") ||
            data.StartsWith("admin_list_employees") || data.StartsWith("admin_employee_statistics") ||
            data.StartsWith("admin_employee_status") || data.StartsWith("set_employee_role_") ||
            data.StartsWith("stats_period_"))
        {
            await RouteToEmployeeHandler(callbackQuery, cancellationToken);
        }
        else if (data.StartsWith("admin_manage_products") || data.StartsWith("admin_add_product") ||
                 data.StartsWith("admin_list_products") || data.StartsWith("set_product_unit_") ||
                 data.StartsWith("delete_product_") || data.StartsWith("toggle_product_") ||
                 data.StartsWith("confirm_delete_product_") || data.StartsWith("edit_product_"))
        {
            await RouteToProductHandler(callbackQuery, cancellationToken);
        }
        else if (data.StartsWith("admin_manage_categories") || data.StartsWith("admin_add_category") ||
                 data.StartsWith("admin_list_categories") || data.StartsWith("delete_category_") ||
                 data.StartsWith("toggle_category_") || data.StartsWith("confirm_delete_category_") ||
                 data.StartsWith("edit_category_"))
        {
            await RouteToCategoryHandler(callbackQuery, cancellationToken);
        }
        else if (data.StartsWith("admin_manage_rooms") || data.StartsWith("admin_add_room") ||
                 data.StartsWith("admin_list_rooms") || data.StartsWith("delete_room_") ||
                 data.StartsWith("toggle_room_") || data.StartsWith("confirm_delete_room_") ||
                 data.StartsWith("edit_room_"))
        {
            await RouteToRoomHandler(callbackQuery, cancellationToken);
        }
        else
        {
            // Обработка общих команд админ-панели
            switch (data)
            {
                case "admin_back_to_main":
                    await _botClient.EditMessageTextAsync(
                        chatId: chatId,
                        messageId: callbackQuery.Message.MessageId,
                        text: "Вы вернулись в главное меню.",
                        replyMarkup: null,
                        cancellationToken: cancellationToken);
                    break;
                case "admin_back_to_admin_menu":
                    await _botClient.EditMessageTextAsync(
                        chatId: chatId,
                        messageId: callbackQuery.Message.MessageId,
                        text: "Вы вернулись в админ-панель.",
                        replyMarkup: KeyboardBuilder.AdminMainMenuKeyboard(),
                        cancellationToken: cancellationToken);
                    break;
                case "admin_cancel":
                    _userStateManager.ClearState(userId);
                    await _botClient.EditMessageTextAsync(
                        chatId: chatId,
                        messageId: callbackQuery.Message.MessageId,
                        text: "Операция отменена. Вы вернулись в главное меню.",
                        replyMarkup: null,
                        cancellationToken: cancellationToken);
                    break;
                default:
                    // Неизвестная команда
                    break;
            }
        }
    }

    private async Task RouteToEmployeeHandler(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        await _employeeAdminHandler.HandleAdminCallbackQuery(callbackQuery, cancellationToken);
    }

    private async Task RouteToProductHandler(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        await _productAdminHandler.HandleAdminCallbackQuery(callbackQuery, cancellationToken);
    }

    private async Task RouteToCategoryHandler(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        await _categoryAdminHandler.HandleAdminCallbackQuery(callbackQuery, cancellationToken);
    }

    private async Task RouteToRoomHandler(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        await _roomAdminHandler.HandleAdminCallbackQuery(callbackQuery, cancellationToken);
    }

    public async Task HandleAdminTextMessageAsync(Message message, CancellationToken cancellationToken)
    {
        var userId = message.From!.Id;
        var currentState = _userStateManager.GetState(userId);

        // Маршрутизация текстовых сообщений к соответствующим handler'ам
        if (currentState >= UserState.AdminAddingEmployeeTelegramId && currentState <= UserState.AdminSelectingEmployeeRole)
        {
            await _employeeAdminHandler.HandleAdminTextMessageAsync(message, cancellationToken);
        }
        else if (currentState == UserState.AdminSelectingStatisticsStartDate || currentState == UserState.AdminSelectingStatisticsEndDate)
        {
            await _employeeAdminHandler.HandleAdminTextMessageAsync(message, cancellationToken);
        }
        else if (currentState >= UserState.AdminAddingCategoryName && currentState <= UserState.AdminAddingCategoryDisplayOrder)
        {
            await _categoryAdminHandler.HandleAdminTextMessageAsync(message, cancellationToken);
        }
        else if (currentState >= UserState.AdminAddingProductCategory && currentState <= UserState.AdminAddingProductDisplayOrder)
        {
            await _productAdminHandler.HandleAdminTextMessageAsync(message, cancellationToken);
        }
        else if (currentState >= UserState.AdminAddingRoomName && currentState <= UserState.AdminAddingRoomPhotoUrl)
        {
            await _roomAdminHandler.HandleAdminTextMessageAsync(message, cancellationToken);
        }
        else
        {
            await _botClient.SendTextMessageAsync(message.Chat.Id, "Неизвестная команда. Пожалуйста, используйте кнопки.", replyMarkup: KeyboardBuilder.AdminMainMenuKeyboard(), cancellationToken: cancellationToken);
        }
    }
}
