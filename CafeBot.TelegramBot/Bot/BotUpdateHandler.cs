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

namespace CafeBot.TelegramBot.Bot;

public class BotUpdateHandler : IUpdateHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;
    private readonly IUserStateManager _stateManager;

    public BotUpdateHandler(IServiceProvider serviceProvider, ILogger logger, IUserStateManager stateManager)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _stateManager = stateManager;
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
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "🏠 Функция просмотра комнат в разработке...",
                cancellationToken: cancellationToken
            );
            return;
        }

        if (messageText == "ℹ️ Помощь")
        {
            await commandHandler.HandleCommandAsync(new Message { Text = "/help", Chat = message.Chat, From = message.From }, cancellationToken);
            return;
        }

        // Обрабатываем текстовый ввод в зависимости от состояния
        var currentState = _stateManager.GetState(userId);
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