using CafeBot.TelegramBot.States;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using CafeBot.Application.Services; 

namespace CafeBot.TelegramBot.Bot;

public class BotBackgroundService : BackgroundService
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<BotBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IUserStateManager _stateManager;

    public BotBackgroundService(
        ITelegramBotClient botClient,
        ILogger<BotBackgroundService> logger,
        IServiceProvider serviceProvider,
        IUserStateManager stateManager)
    {
        _botClient = botClient;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _stateManager = stateManager;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var me = await _botClient.GetMeAsync(stoppingToken);
        _logger.LogInformation("Бот запущен: @{BotUsername}", me.Username);

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[]
            {
                UpdateType.Message,
                UpdateType.CallbackQuery
            }
        };

        // Создаем отдельный scope для BotUpdateHandler
        using var scope = _serviceProvider.CreateScope();
        var updateHandler = scope.ServiceProvider.GetRequiredService<BotUpdateHandler>();

        await _botClient.ReceiveAsync(
            updateHandler: updateHandler,
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken
        );
    }
}