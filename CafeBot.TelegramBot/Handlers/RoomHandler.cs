using CafeBot.Application.Services;
using CafeBot.Core.Enums;
using CafeBot.TelegramBot.Keyboards;
using CafeBot.TelegramBot.States;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CafeBot.TelegramBot.Handlers;

public class RoomHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly IUserStateManager _userStateManager;
    private readonly IRoomService _roomService;

    public RoomHandler(ITelegramBotClient botClient, IUserStateManager userStateManager, IRoomService roomService)
    {
        _botClient = botClient;
        _userStateManager = userStateManager;
        _roomService = roomService;
    }

    public async Task HandleRoomCommand(Message message, CancellationToken cancellationToken)
    {
        await ShowRooms(message.Chat.Id, cancellationToken);
    }

    private async Task ShowRooms(long chatId, CancellationToken cancellationToken)
    {
        var rooms = await _roomService.GetActiveRoomsAsync();

        if (!rooms.Any())
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Hozirda mavjud xonalar yo'q.",
                cancellationToken: cancellationToken);
            return;
        }

        var roomList = new System.Text.StringBuilder();
        roomList.AppendLine("Xonalar ro'yxati:");
        roomList.AppendLine();

        foreach (var room in rooms)
        {
            roomList.AppendLine($"🏢 {room.Name} (Вместимость: {room.Capacity}) - Статус: {room.Status switch
            {
                RoomStatus.Active => "✅ Активна",
                RoomStatus.Inactive => "❌ Неактивна",
                RoomStatus.Maintenance => "🛠️ На обслуживании",
                _ => "Noma'lum"
            }}");
            roomList.AppendLine($"  Tavsif: {room.Description ?? "Tavsif yo'q"}");
            roomList.AppendLine();
        }

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: roomList.ToString(),
            parseMode: ParseMode.Html,
            replyMarkup: KeyboardBuilder.MainMenuKeyboard(),
            cancellationToken: cancellationToken);
    }
}

