using CafeBot.Core.Interfaces;
using CafeBot.TelegramBot.Handlers;
using CafeBot.TelegramBot.Keyboards;
using CafeBot.TelegramBot.States;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using CafeBot.Core.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using CafeBot.Application.Services;

namespace CafeBot.TelegramBot.Bot;

public class BotUpdateHandler : IUpdateHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BotUpdateHandler> _logger; // Изменено на типизированный логгер
    private readonly IUserStateManager _stateManager;
    private readonly IEmployeeService _employeeService; 

    public BotUpdateHandler(IServiceProvider serviceProvider, ILogger<BotUpdateHandler> logger, IUserStateManager stateManager, IEmployeeService employeeService)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _stateManager = stateManager;
        _employeeService = employeeService; 
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try
        {
            var handler = update switch
            {
                { Message: { } message } => HandleMessageAsync(botClient, message, cancellationToken),
                { CallbackQuery: { } callbackQuery } => HandleCallbackQueryAsync(botClient, callbackQuery, cancellationToken),
                _ => Task.CompletedTask
            };

            await handler;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке update");
        }
    }

    private async Task HandleMessageAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        if (message.From == null)
            return;

        var userId = message.From.Id;
        var chatId = message.Chat.Id;
        var messageText = message.Text ?? string.Empty;

        _logger.LogInformation("Получено сообщение: {Text} от {UserId}", messageText, userId);

        using var scope = _serviceProvider.CreateScope();
        var commandHandler = scope.ServiceProvider.GetRequiredService<CommandHandler>();
        var orderFlowHandler = scope.ServiceProvider.GetRequiredService<OrderFlowHandler>();
        var roomHandler = scope.ServiceProvider.GetRequiredService<RoomHandler>();
        var adminHandler = scope.ServiceProvider.GetRequiredService<AdminHandler>(); 
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        // Проверяем команды
        if (messageText.StartsWith("/"))
        {
            await commandHandler.HandleCommandAsync(message, cancellationToken);
            return;
        }

        // Проверяем кнопки главного меню
        if (messageText == "🆕 Создать заказ")
        {
            var employee = await unitOfWork.Employees.GetByTelegramIdAsync(userId);
            if (employee == null || !employee.IsActive)
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "❌ У вас нет доступа к боту.",
                    cancellationToken: cancellationToken
                );
                return;
            }

            await orderFlowHandler.StartOrderCreationAsync(chatId, userId, cancellationToken);
            return;
        }

        if (messageText == "📝 Мои заказы")
        {
            var orderListHandler = scope.ServiceProvider.GetRequiredService<OrderListHandler>();
            await orderListHandler.ShowMyOrdersAsync(chatId, userId, cancellationToken);
            return;
        }

        if (messageText == "🏠 Комнаты")
        {
            await roomHandler.HandleRoomCommand(message, cancellationToken);
            return;
        }

        if (messageText == "ℹ️ Помощь")
        {
            await commandHandler.HandleCommandAsync(new Message { Text = "/help", Chat = message.Chat, From = message.From }, cancellationToken);
            return;
        }
        
        // Обработка кнопки "Админ панель"
        if (messageText == "🔧 Админ панель")
        {
            var employee = await _employeeService.GetEmployeeByTelegramIdAsync(userId);
            if (employee?.Role == EmployeeRole.Admin && employee.IsActive)
            {
                await adminHandler.HandleAdminPanelCommand(message, cancellationToken);
            }
            else
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "❌ У вас нет прав доступа к админ-панели.",
                    replyMarkup: KeyboardBuilder.MainMenuKeyboard(), 
                    cancellationToken: cancellationToken
                );
            }
            return;
        }

        // Обрабатываем текстовый ввод в зависимости от состояния
        var currentState = _stateManager.GetState(userId);
        if (currentState >= UserState.AdminAddingEmployeeTelegramId && currentState <= UserState.AdminAddingRoomPhotoUrl)
        {
            await adminHandler.HandleAdminTextMessageAsync(message, cancellationToken);
            return;
        }
        if (currentState != UserState.None)
        {
            await orderFlowHandler.HandleTextMessageAsync(message, userId, cancellationToken);
            return;
        }

        // Неизвестное сообщение
        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "❓ Используйте кнопки меню или команды.",
            replyMarkup: KeyboardBuilder.MainMenuKeyboard(),
            cancellationToken: cancellationToken
        );
    }

    private async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        if (callbackQuery.From == null || callbackQuery.Message == null)
            return;

        var userId = callbackQuery.From.Id;
        var chatId = callbackQuery.Message.Chat.Id;
        var data = callbackQuery.Data ?? string.Empty;

        _logger.LogInformation("Получен callback: {Data} от {UserId}", data, userId);

        using var scope = _serviceProvider.CreateScope();
        var orderFlowHandler = scope.ServiceProvider.GetRequiredService<OrderFlowHandler>();
        var orderListHandler = scope.ServiceProvider.GetRequiredService<OrderListHandler>();
        var adminHandler = scope.ServiceProvider.GetRequiredService<AdminHandler>(); 

        // Обработка callback-ов админ-панели
        if (data.StartsWith("admin_"))
        {
            var employee = await _employeeService.GetEmployeeByTelegramIdAsync(userId);
            if (employee?.Role == EmployeeRole.Admin && employee.IsActive)
            {
                await adminHandler.HandleAdminCallbackQuery(callbackQuery, cancellationToken);
            }
            else
            {
                await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ У вас нет прав доступа к админ-панели.", showAlert: true, cancellationToken: cancellationToken);
                // Возможно, обновить клавиатуру, чтобы скрыть админ-кнопку, если она была видна по ошибке
                await botClient.EditMessageReplyMarkupAsync(chatId, callbackQuery.Message.MessageId, replyMarkup: null, cancellationToken: cancellationToken); 
            }
            return;
        }

        // Обработка callback-ов для установки роли сотрудника
        if (data.StartsWith("set_employee_role_"))
        {
            var employee = await _employeeService.GetEmployeeByTelegramIdAsync(userId);
            if (employee?.Role == EmployeeRole.Admin && employee.IsActive)
            {
                await adminHandler.HandleAdminCallbackQuery(callbackQuery, cancellationToken);
            }
            else
            {
                await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ У вас нет прав доступа к админ-панели.", showAlert: true, cancellationToken: cancellationToken);
                await botClient.EditMessageReplyMarkupAsync(chatId, callbackQuery.Message.MessageId, replyMarkup: null, cancellationToken: cancellationToken);
            }
            return;
        }

        // Обработка callback-ов для установки единицы измерения продукта
        if (data.StartsWith("set_product_unit_"))
        {
            var employee = await _employeeService.GetEmployeeByTelegramIdAsync(userId);
            if (employee?.Role == EmployeeRole.Admin && employee.IsActive)
            {
                await adminHandler.HandleAdminCallbackQuery(callbackQuery, cancellationToken);
            }
            else
            {
                await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ У вас нет прав доступа к админ-панели.", showAlert: true, cancellationToken: cancellationToken);
                await botClient.EditMessageReplyMarkupAsync(chatId, callbackQuery.Message.MessageId, replyMarkup: null, cancellationToken: cancellationToken);
            }
            return;
        }

        // Обработка callback-ов статистики
        if (data.StartsWith("stats_period_"))
        {
            var employee = await _employeeService.GetEmployeeByTelegramIdAsync(userId);
            if (employee?.Role == EmployeeRole.Admin && employee.IsActive)
            {
                await adminHandler.HandleAdminCallbackQuery(callbackQuery, cancellationToken);
            }
            else
            {
                await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ У вас нет прав доступа к админ-панели.", showAlert: true, cancellationToken: cancellationToken);
                await botClient.EditMessageReplyMarkupAsync(chatId, callbackQuery.Message.MessageId, replyMarkup: null, cancellationToken: cancellationToken);
            }
            return;
        }

        // Обработка просмотра заказов
        if (data.StartsWith("vieworder_"))
        {
            var orderIdStr = data.Replace("vieworder_", "");
            if (int.TryParse(orderIdStr, out var orderId))
            {
                await orderListHandler.ShowOrderDetailsAsync(chatId, orderId, cancellationToken);
            }
            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
            return;
        }

        // Обработка дозаказа
        if (data.StartsWith("addmore_"))
        {
            var orderIdStr = data.Replace("addmore_", "");
            if (int.TryParse(orderIdStr, out var orderId))
            {
                await orderFlowHandler.StartAddingItemsToOrderAsync(chatId, userId, orderId, cancellationToken);
            }
            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
            return;
        }

        // Обработка перехода к оплате
        if (data.StartsWith("topayment_"))
        {
            var orderIdStr = data.Replace("topayment_", "");
            if (int.TryParse(orderIdStr, out var orderId))
            {
                var paymentHandler = scope.ServiceProvider.GetRequiredService<PaymentHandler>();
                await paymentHandler.PrepareOrderForPaymentAsync(chatId, orderId, cancellationToken);
            }
            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
            return;
        }

        // Обработка выбора способа оплаты
        if (data.StartsWith("pay_"))
        {
            var parts = data.Split('_');
            if (parts.Length == 3 && int.TryParse(parts[2], out var orderId))
            {
                var method = parts[1] switch
                {
                    "cash" => PaymentMethod.Cash,
                    "card" => PaymentMethod.Card,
                    "transfer" => PaymentMethod.Transfer,
                    _ => PaymentMethod.Cash
                };

                var paymentHandler = scope.ServiceProvider.GetRequiredService<PaymentHandler>();
                await paymentHandler.ProcessPaymentAsync(chatId, userId, orderId, method, cancellationToken);
            }
            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
            return;
        }

        // Обработка подтверждения заказа
        if (data.StartsWith("confirm_order_"))
        {
            var orderIdStr = data.Replace("confirm_order_", "");
            if (int.TryParse(orderIdStr, out var orderId))
            {
                var managementHandler = scope.ServiceProvider.GetRequiredService<OrderManagementHandler>();
                await managementHandler.ConfirmOrderAsync(chatId, orderId, cancellationToken);
            }
            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
            return;
        }

        // Обработка активации заказа
        if (data.StartsWith("activate_order_"))
        {
            var orderIdStr = data.Replace("activate_order_", "");
            if (int.TryParse(orderIdStr, out var orderId))
            {
                var managementHandler = scope.ServiceProvider.GetRequiredService<OrderManagementHandler>();
                await managementHandler.ActivateOrderAsync(chatId, orderId, cancellationToken);
            }
            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
            return;
        }

        // Обработка отмены заказа
        if (data.StartsWith("cancel_order_"))
        {
            var orderIdStr = data.Replace("cancel_order_", "");
            if (int.TryParse(orderIdStr, out var orderId))
            {
                var managementHandler = scope.ServiceProvider.GetRequiredService<OrderManagementHandler>();
                await managementHandler.CancelOrderAsync(chatId, userId, orderId, cancellationToken);
            }
            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
            return;
        }

        if (data == "refresh_orders" || data == "back_to_orders")
        {
            await orderListHandler.ShowMyOrdersAsync(chatId, userId, cancellationToken);
            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
            return;
        }

        // Остальные callback передаем в orderFlowHandler
        await orderFlowHandler.HandleCallbackAsync(callbackQuery, userId, cancellationToken);
    }

    public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Ошибка polling");
        return Task.CompletedTask;
    }
}