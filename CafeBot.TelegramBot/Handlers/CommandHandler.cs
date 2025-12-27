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
                    text: "❓ Noma'lum buyruq. Buyruqlar ro'yxati uchun /help dan foydalaning.",
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
                text: "❌ Sizda botga kirish huquqi yo'q.\n\n" +
                      "Kirish huquqini olish uchun administrator bilan bog'laning.",
                cancellationToken: cancellationToken
            );
            return;
        }

        // Очищаем состояние
        _stateManager.ClearState(telegramId);

        var isAdmin = employee.Role == Core.Enums.EmployeeRole.Admin;
        var greeting = $"👋 Xush kelibsiz, {employee.FirstName}!\n\n";

        if (isAdmin)
        {
            greeting += "🔧 Siz admin sifatida kirdingiz.\n";
        }
        else
        {
            greeting += "👔 Siz ofitsiant sifatida kirdingiz.\n";
        }

        greeting += "\nHarakatni tanlang:";

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: greeting,
            replyMarkup: KeyboardBuilder.MainMenuKeyboard(isAdmin),
            cancellationToken: cancellationToken
        );
    }

    private async Task HandleHelpCommandAsync(long chatId, CancellationToken cancellationToken)
    {
        var helpText = @"📋 Botdan foydalanish bo'yicha yo'riqnoma

🆕 Buyurtma yaratish - yangi bron qilishni boshlash
📝 Mening buyurtmalarim - faol buyurtmalarni ko'rish
🏠 Xonalar - barcha xonalar ro'yxati
ℹ️ Yordam - ushbu yo'riqnoma

Ishni boshlash uchun /start dan foydalaning";

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: helpText,
            cancellationToken: cancellationToken
        );
    }
}