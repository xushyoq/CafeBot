using CafeBot.Core.Interfaces;
using CafeBot.TelegramBot.Keyboards;
using CafeBot.TelegramBot.States;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CafeBot.TelegramBot.Handlers;

public class CommandHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserStateManager _stateManager;
    private readonly ILogger<CommandHandler> _logger;

    public CommandHandler(
        ITelegramBotClient botClient,
        IUnitOfWork unitOfWork,
        IUserStateManager stateManager,
        ILogger<CommandHandler> logger)
    {
        _botClient = botClient;
        _unitOfWork = unitOfWork;
        _stateManager = stateManager;
        _logger = logger;
    }

    public async Task HandleCommandAsync(Message message, CancellationToken cancellationToken)
    {
        var text = message.Text ?? string.Empty;
        var chatId = message.Chat.Id;
        var telegramId = message.From?.Id ?? 0;

        _logger.LogInformation("Command received: {Command} from {TelegramId}", text, telegramId);

        switch (text)
        {
            case "/start":
                await HandleStartCommandAsync(chatId, telegramId, cancellationToken);
                break;

            case "/help":
                await HandleHelpCommandAsync(chatId, cancellationToken);
                break;

            default:
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "❓ Неизвестная команда. Используйте /help для списка команд.",
                    cancellationToken: cancellationToken
                );
                break;
        }
    }

    private async Task HandleStartCommandAsync(long chatId, long telegramId, CancellationToken cancellationToken)
    {
        // Проверяем есть ли пользователь в базе как Employee
        var employee = await _unitOfWork.Employees.GetByTelegramIdAsync(telegramId);

        if (employee == null || !employee.IsActive)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ У вас нет доступа к боту.\n\n" +
                      "Обратитесь к администратору для получения доступа.",
                cancellationToken: cancellationToken
            );
            return;
        }

        // Очищаем состояние
        _stateManager.ClearState(telegramId);

        var isAdmin = employee.Role == Core.Enums.EmployeeRole.Admin;
        var greeting = $"👋 Добро пожаловать, {employee.FirstName}!\n\n";

        if (isAdmin)
        {
            greeting += "🔧 Вы вошли как администратор.\n";
        }
        else
        {
            greeting += "👔 Вы вошли как официант.\n";
        }

        greeting += "\nВыберите действие:";

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: greeting,
            replyMarkup: KeyboardBuilder.MainMenuKeyboard(isAdmin),
            cancellationToken: cancellationToken
        );
    }

    private async Task HandleHelpCommandAsync(long chatId, CancellationToken cancellationToken)
    {
        var helpText = @"📋 Справка по использованию бота

🆕 Создать заказ - начать новое бронирование
📝 Мои заказы - просмотр активных заказов
🏠 Комнаты - список всех комнат
ℹ️ Помощь - эта справка

Для начала работы используйте /start";

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: helpText,
            cancellationToken: cancellationToken
        );
    }
}