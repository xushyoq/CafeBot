using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CafeBot.TelegramBot.Bot;

public class BotUpdateHandler : IUpdateHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;

    public BotUpdateHandler(IServiceProvider serviceProvider, ILogger logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
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
        if (message.Text is not { } messageText)
            return;

        var chatId = message.Chat.Id;
        _logger.LogInformation("Получено сообщение: {Text} от {ChatId}", messageText, chatId);

        var response = messageText switch
        {
            "/start" => "👋 Добро пожаловать в CafeBot!\n\n" +
                       "Я помогу вам управлять заказами и бронированиями.\n\n" +
                       "Используйте команды:\n" +
                       "/rooms - Посмотреть комнаты\n" +
                       "/orders - Активные заказы\n" +
                       "/help - Помощь",
            
            "/help" => "📋 Доступные команды:\n\n" +
                      "/start - Начать работу\n" +
                      "/rooms - Список комнат\n" +
                      "/orders - Активные заказы\n" +
                      "/help - Эта справка",
            
            "/rooms" => "🏠 Функция просмотра комнат в разработке...",
            "/orders" => "📝 Функция просмотра заказов в разработке...",
            
            _ => "❓ Неизвестная команда. Используйте /help для списка команд."
        };

        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: response,
            cancellationToken: cancellationToken
        );
    }

    private async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Получен callback: {Data}", callbackQuery.Data);

        await botClient.AnswerCallbackQueryAsync(
            callbackQueryId: callbackQuery.Id,
            text: "В разработке...",
            cancellationToken: cancellationToken
        );
    }

    public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Ошибка polling");
        return Task.CompletedTask;
    }
}