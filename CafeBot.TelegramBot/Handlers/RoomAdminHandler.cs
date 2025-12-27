using CafeBot.Application.Services;
using CafeBot.Core.Enums;
using CafeBot.TelegramBot.Keyboards;
using CafeBot.TelegramBot.States;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace CafeBot.TelegramBot.Handlers;

public class RoomAdminHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly IUserStateManager _userStateManager;
    private readonly IRoomService _roomService;

    public RoomAdminHandler(ITelegramBotClient botClient, IUserStateManager userStateManager, IRoomService roomService)
    {
        _botClient = botClient;
        _userStateManager = userStateManager;
        _roomService = roomService;
    }

    public async Task ShowManageRoomsMenu(long chatId, CancellationToken cancellationToken)
    {
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Xonalarni boshqarish:",
            replyMarkup: KeyboardBuilder.ManageRoomsKeyboard(),
            cancellationToken: cancellationToken);
    }

    public async Task StartAddRoomFlow(long chatId, long userId, CancellationToken cancellationToken)
    {
        _userStateManager.SetState(userId, UserState.AdminAddingRoomName);
        _userStateManager.ClearStateData(userId);

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Xona nomini kiriting:",
            replyMarkup: new ForceReplyMarkup { Selective = true },
            cancellationToken: cancellationToken);
    }

    public async Task HandleRoomNameInput(long chatId, long userId, string roomName, CancellationToken cancellationToken)
    {
        var stateData = _userStateManager.GetStateData(userId);
        stateData.AdminRoomName = roomName;

        _userStateManager.SetState(userId, UserState.AdminAddingRoomNumber);
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Xona raqamini kiriting (raqam, yoki '-' belgisi bilan o'tkazib yuborish uchun):",
            replyMarkup: new ForceReplyMarkup { Selective = true },
            cancellationToken: cancellationToken);
    }

    public async Task HandleRoomNumberInput(long chatId, long userId, string roomNumberText, CancellationToken cancellationToken)
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
                text: "❌ Raqam formati noto'g'ri. Musbat raqam yoki '-' belgisi bilan o'tkazib yuborish uchun kiriting:",
                cancellationToken: cancellationToken);
            return;
        }

        _userStateManager.SetState(userId, UserState.AdminAddingRoomCapacity);
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Xona sig'imini kiriting (odamlar soni, raqam):",
            replyMarkup: new ForceReplyMarkup { Selective = true },
            cancellationToken: cancellationToken);
    }

    public async Task HandleRoomCapacityInput(long chatId, long userId, string capacityText, CancellationToken cancellationToken)
    {
        if (!int.TryParse(capacityText, out var capacity) || capacity <= 0)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Sig'im formati noto'g'ri. Musbat raqam kiriting:",
                cancellationToken: cancellationToken);
            return;
        }

        var stateData = _userStateManager.GetStateData(userId);
        stateData.AdminRoomCapacity = capacity;

        _userStateManager.SetState(userId, UserState.AdminAddingRoomDescription);
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Xona tavsifini kiriting (yoki '-' belgisi bilan o'tkazib yuborish uchun):",
            replyMarkup: new ForceReplyMarkup { Selective = true },
            cancellationToken: cancellationToken);
    }

    public async Task HandleRoomDescriptionInput(long chatId, long userId, string description, CancellationToken cancellationToken)
    {
        var stateData = _userStateManager.GetStateData(userId);
        stateData.AdminRoomDescription = description == "-" ? null : description;

        _userStateManager.SetState(userId, UserState.AdminAddingRoomPhotoUrl);
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Xona rasmi URL manzilini kiriting (yoki '-' belgisi bilan o'tkazib yuborish uchun):",
            replyMarkup: new ForceReplyMarkup { Selective = true },
            cancellationToken: cancellationToken);
    }

    public async Task HandleRoomPhotoUrlInput(long chatId, long userId, string photoUrl, CancellationToken cancellationToken)
    {
        var stateData = _userStateManager.GetStateData(userId);

        // Проверяем, что все обязательные поля заполнены
        if (string.IsNullOrEmpty(stateData.AdminRoomName) || !stateData.AdminRoomCapacity.HasValue)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Xatolik: barcha ma'lumotlar to'ldirilmagan. Qaytadan boshlang.",
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
                text: $"✅ '{room.Name}' xonasi muvaffaqiyatli yaratildi!",
                replyMarkup: KeyboardBuilder.ManageRoomsKeyboard(),
                cancellationToken: cancellationToken);

            _userStateManager.ClearState(userId);
        }
        catch (Exception ex)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"❌ Xona yaratishda xatolik: {ex.Message}",
                replyMarkup: KeyboardBuilder.ManageRoomsKeyboard(),
                cancellationToken: cancellationToken);
            _userStateManager.ClearState(userId);
        }
    }

    public async Task ShowRoomList(long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var rooms = await _roomService.GetActiveRoomsAsync();

            if (!rooms.Any())
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "🏠 Hozirda faol xonalar yo'q.",
                    replyMarkup: KeyboardBuilder.ManageRoomsKeyboard(),
                    cancellationToken: cancellationToken);
                return;
            }

            // Отправляем заголовок
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "🏠 Xonalar ro'yxati:",
                replyMarkup: KeyboardBuilder.ManageRoomsKeyboard(),
                cancellationToken: cancellationToken);

            // Отправляем каждую комнату отдельно с кнопками
            foreach (var room in rooms.OrderBy(r => r.Number ?? 999).ThenBy(r => r.Name))
            {
                var message = $"🏠 {room.Name}";
                if (room.Number.HasValue)
                    message += $" (№{room.Number})";
                message += $"\n";
                message += $"   Sig'im: {room.Capacity} kishi\n";
                message += $"   Holat: {(room.Status == RoomStatus.Active ? "✅ Faol" : "❌ Faol emas")}\n";
                if (!string.IsNullOrEmpty(room.Description))
                    message += $"   Tavsif: {room.Description}\n";
                message += $"   ID: {room.Id}";

                var buttons = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("✏️ Tahrirlash", $"edit_room_{room.Id}"),
                        InlineKeyboardButton.WithCallbackData("🗑️ O'chirish", $"delete_room_{room.Id}")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🔄 Holat", $"toggle_room_{room.Id}")
                    }
                });

                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: message,
                    replyMarkup: buttons,
                    cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"❌ Xonalar ro'yxatini olishda xatolik: {ex.Message}",
                replyMarkup: KeyboardBuilder.ManageRoomsKeyboard(),
                cancellationToken: cancellationToken);
        }
    }

    public async Task HandleEditRoomCallback(long userId, long chatId, string callbackData, int messageId, CancellationToken cancellationToken)
    {
        var parts = callbackData.Split('_');
        if (parts.Length == 3 && int.TryParse(parts[2], out var roomId))
        {
            var room = await _roomService.GetRoomByIdAsync(roomId);
            if (room == null)
            {
                return;
            }

            // Сохраняем ID комнаты для редактирования
            var stateData = _userStateManager.GetStateData(userId);
            stateData.AdminEditingRoomId = roomId;

            await _botClient.EditMessageTextAsync(
                chatId: chatId,
                messageId: messageId,
                text: $"✏️ Xona '{room.Name}'ni tahrirlash\n\nJoriy ma'lumotlar:\nNomi: {room.Name}\nRaqam: {room.Number?.ToString() ?? "Yo'q"}\nSig'im: {room.Capacity} kishi\nTavsif: {room.Description ?? "Yo'q"}\nHolat: {(room.Status == RoomStatus.Active ? "Faol" : "Faol emas")}\n\nNima o'zgartirmoqchisiz?",
                replyMarkup: new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("📝 Nomi", $"edit_room_name_{roomId}"),
                        InlineKeyboardButton.WithCallbackData("🔢 Raqam", $"edit_room_number_{roomId}")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("👥 Sig'im", $"edit_room_capacity_{roomId}"),
                        InlineKeyboardButton.WithCallbackData("📄 Tavsif", $"edit_room_desc_{roomId}")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🔄 Holat", $"toggle_room_{roomId}")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("⬅️ Orqaga", "admin_list_rooms")
                    }
                }),
                cancellationToken: cancellationToken);
        }
    }

    public async Task HandleDeleteRoomCallback(long userId, long chatId, string callbackData, int messageId, CancellationToken cancellationToken)
    {
        var parts = callbackData.Split('_');
        if (parts.Length == 3 && int.TryParse(parts[2], out var roomId))
        {
            var room = await _roomService.GetRoomByIdAsync(roomId);
            if (room == null)
            {
                return;
            }

            await _botClient.EditMessageTextAsync(
                chatId: chatId,
                messageId: messageId,
                text: $"🗑️ Siz haqiqatan ham '{room.Name}' xonasini o'chirmoqchimisiz?\n\n⚠️ Bu harakatni bekor qilib bo'lmaydi!\n⚠️ Bu xonadagi barcha faol buyurtmalar bekor qilinadi!",
                replyMarkup: KeyboardBuilder.YesNoKeyboard("confirm_delete_room", roomId),
                cancellationToken: cancellationToken);
        }
    }

    public async Task HandleToggleRoomCallback(long userId, long chatId, string callbackData, int messageId, CancellationToken cancellationToken)
    {
        var parts = callbackData.Split('_');
        if (parts.Length == 3 && int.TryParse(parts[2], out var roomId))
        {
            try
            {
                var room = await _roomService.GetRoomByIdAsync(roomId);
                if (room == null)
                {
                    return;
                }

                var success = await _roomService.ToggleRoomStatusAsync(roomId);

                if (success)
                {
                    var newStatus = room.Status == RoomStatus.Active ? RoomStatus.Inactive : RoomStatus.Active;
                    await _botClient.EditMessageTextAsync(
                        chatId: chatId,
                        messageId: messageId,
                        text: $"✅ Xona '{room.Name}' endi {(newStatus == RoomStatus.Active ? "faol" : "faol emas")}.",
                        replyMarkup: new InlineKeyboardMarkup(new[]
                        {
                            InlineKeyboardButton.WithCallbackData("⬅️ Xonalar ro'yxatiga", "admin_list_rooms")
                        }),
                        cancellationToken: cancellationToken);
                }
            }
            catch (Exception)
            {
                // Callback ответ не требуется
            }
        }
    }

    public async Task HandleConfirmDeleteRoom(long userId, long chatId, string callbackData, int messageId, CancellationToken cancellationToken)
    {
        var parts = callbackData.Split('_');
        if (parts.Length == 5 && int.TryParse(parts[4], out var roomId))
        {
            try
            {
                var room = await _roomService.GetRoomByIdAsync(roomId);
                var roomName = room?.Name ?? "Noma'lum xona";

                var success = await _roomService.DeleteRoomAsync(roomId);

                if (success)
                {
                    await _botClient.EditMessageTextAsync(
                        chatId: chatId,
                        messageId: messageId,
                        text: $"✅ '{roomName}' xonasi muvaffaqiyatli o'chirildi!",
                        replyMarkup: new InlineKeyboardMarkup(new[]
                        {
                            InlineKeyboardButton.WithCallbackData("⬅️ Xonalar ro'yxatiga", "admin_list_rooms")
                        }),
                        cancellationToken: cancellationToken);
                }
                else
                {
                    await _botClient.EditMessageTextAsync(
                        chatId: chatId,
                        messageId: messageId,
                        text: "❌ Xonani o'chirib bo'lmadi.",
                        replyMarkup: new InlineKeyboardMarkup(new[]
                        {
                            InlineKeyboardButton.WithCallbackData("⬅️ Xonalar ro'yxatiga", "admin_list_rooms")
                        }),
                        cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await _botClient.EditMessageTextAsync(
                    chatId: chatId,
                    messageId: messageId,
                    text: $"❌ Xona o'chirishda xatolik: {ex.Message}",
                    replyMarkup: new InlineKeyboardMarkup(new[]
                    {
                        InlineKeyboardButton.WithCallbackData("⬅️ Xonalar ro'yxatiga", "admin_list_rooms")
                    }),
                    cancellationToken: cancellationToken);
            }
        }
    }

    public async Task HandleCancelDeleteRoom(long userId, long chatId, string callbackData, int messageId, CancellationToken cancellationToken)
    {
        await _botClient.EditMessageTextAsync(
            chatId: chatId,
            messageId: messageId,
            text: "❌ Xona o'chirish bekor qilindi.",
            replyMarkup: new InlineKeyboardMarkup(new[]
            {
                InlineKeyboardButton.WithCallbackData("⬅️ Xonalar ro'yxatiga", "admin_list_rooms")
            }),
            cancellationToken: cancellationToken);
    }

    public async Task HandleRoomEditField(string callbackQueryId, long userId, long chatId, string callbackData, int messageId, CancellationToken cancellationToken)
    {
        // Ответ на callback
        await _botClient.AnswerCallbackQueryAsync(callbackQueryId, cancellationToken: cancellationToken);

        var parts = callbackData.Split('_');
        if (parts.Length == 4 && int.TryParse(parts[3], out var roomId))
        {
            var field = parts[2]; // name, number, capacity, desc
            var stateData = _userStateManager.GetStateData(userId);

            // Сохраняем ID редактируемой комнаты
            stateData.AdminEditingRoomId = roomId;

            switch (field)
            {
                case "name":
                    _userStateManager.SetState(userId, UserState.AdminAddingRoomName);
                    await _botClient.EditMessageTextAsync(
                        chatId: chatId,
                        messageId: messageId,
                        text: "Xona uchun yangi nom kiriting:",
                        cancellationToken: cancellationToken);
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Xona uchun yangi nom kiriting:",
                        replyMarkup: new ForceReplyMarkup { Selective = true },
                        cancellationToken: cancellationToken);
                    break;
                case "number":
                    _userStateManager.SetState(userId, UserState.AdminAddingRoomNumber);
                    await _botClient.EditMessageTextAsync(
                        chatId: chatId,
                        messageId: messageId,
                        text: "Xona uchun yangi raqam kiriting (raqam yoki '-' belgisi bilan o'chirish uchun):",
                        cancellationToken: cancellationToken);
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Xona uchun yangi raqam kiriting (raqam yoki '-' belgisi bilan o'chirish uchun):",
                        replyMarkup: new ForceReplyMarkup { Selective = true },
                        cancellationToken: cancellationToken);
                    break;
                case "capacity":
                    _userStateManager.SetState(userId, UserState.AdminAddingRoomCapacity);
                    await _botClient.EditMessageTextAsync(
                        chatId: chatId,
                        messageId: messageId,
                        text: "Xona uchun yangi sig'im kiriting (odamlar soni, raqam):",
                        cancellationToken: cancellationToken);
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Xona uchun yangi sig'im kiriting (odamlar soni, raqam):",
                        replyMarkup: new ForceReplyMarkup { Selective = true },
                        cancellationToken: cancellationToken);
                    break;
                case "desc":
                    _userStateManager.SetState(userId, UserState.AdminAddingRoomDescription);
                    await _botClient.EditMessageTextAsync(
                        chatId: chatId,
                        messageId: messageId,
                        text: "Xona uchun yangi tavsif kiriting (yoki '-' belgisi bilan o'chirish uchun):",
                        cancellationToken: cancellationToken);
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Xona uchun yangi tavsif kiriting (yoki '-' belgisi bilan o'chirish uchun):",
                        replyMarkup: new ForceReplyMarkup { Selective = true },
                        cancellationToken: cancellationToken);
                    break;
                default:
                    await _botClient.EditMessageTextAsync(
                        chatId: chatId,
                        messageId: messageId,
                        text: "Tahrirlash uchun noma'lum maydon.",
                        replyMarkup: KeyboardBuilder.ManageRoomsKeyboard(),
                        cancellationToken: cancellationToken);
                    _userStateManager.ClearState(userId);
                    break;
            }
        }
    }

    public async Task HandleAdminCallbackQuery(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var userId = callbackQuery.From!.Id;
        var chatId = callbackQuery.Message!.Chat.Id;
        var data = callbackQuery.Data ?? string.Empty;

        switch (data)
        {
            case "admin_manage_rooms":
                await ShowManageRoomsMenu(chatId, cancellationToken);
                break;
            case "admin_add_room":
                await StartAddRoomFlow(chatId, userId, cancellationToken);
                break;
            case "admin_list_rooms":
                await ShowRoomList(chatId, cancellationToken);
                break;
            default:
                if (data.StartsWith("delete_room_"))
                {
                    await HandleDeleteRoomCallback(userId, chatId, data, callbackQuery.Message.MessageId, cancellationToken);
                }
                else if (data.StartsWith("toggle_room_"))
                {
                    await HandleToggleRoomCallback(userId, chatId, data, callbackQuery.Message.MessageId, cancellationToken);
                }
                else if (data.StartsWith("confirm_delete_room_yes_"))
                {
                    await HandleConfirmDeleteRoom(userId, chatId, data, callbackQuery.Message.MessageId, cancellationToken);
                }
                else if (data.StartsWith("confirm_delete_room_no_"))
                {
                    await HandleCancelDeleteRoom(userId, chatId, data, callbackQuery.Message.MessageId, cancellationToken);
                }
                else if (data.StartsWith("edit_room_"))
                {
                    var parts = data.Split('_');
                    if (parts.Length == 3) // edit_room_{id} - показ меню редактирования
                    {
                        await HandleEditRoomCallback(userId, chatId, data, callbackQuery.Message.MessageId, cancellationToken);
                    }
                    else // edit_room_{field}_{id} - редактирование конкретного поля
                    {
                        await HandleRoomEditField(callbackQuery.Id, userId, chatId, data, callbackQuery.Message.MessageId, cancellationToken);
                    }
                }
                break;
        }
    }

    public async Task HandleAdminTextMessageAsync(Message message, CancellationToken cancellationToken)
    {
        var userId = message.From!.Id;
        var chatId = message.Chat.Id;
        var messageText = message.Text ?? string.Empty;

        var currentState = _userStateManager.GetState(userId);
        var stateData = _userStateManager.GetStateData(userId);

        // Если есть AdminEditingRoomId, значит это редактирование существующей комнаты
        if (stateData.AdminEditingRoomId.HasValue)
        {
            await HandleRoomEditInput(chatId, userId, currentState, messageText, cancellationToken);
        }
        else
        {
            // Обычное создание комнаты
            switch (currentState)
            {
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
            }
        }
    }

    private async Task HandleRoomEditInput(long chatId, long userId, UserState currentState, string messageText, CancellationToken cancellationToken)
    {
        var stateData = _userStateManager.GetStateData(userId);
        var roomId = stateData.AdminEditingRoomId.Value;

        try
        {
            switch (currentState)
            {
                case UserState.AdminAddingRoomName:
                    await _roomService.UpdateRoomAsync(roomId, messageText, null, null, null, null, null);
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "✅ Xona nomi muvaffaqiyatli yangilandi!",
                        replyMarkup: KeyboardBuilder.ManageRoomsKeyboard(),
                        cancellationToken: cancellationToken);
                    break;
                case UserState.AdminAddingRoomNumber:
                    int? roomNumber = null;
                    if (messageText != "-")
                    {
                        if (!int.TryParse(messageText, out var number))
                        {
                            await _botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "❌ Formati noto'g'ri. Xona raqami uchun raqam yoki '-' belgisi bilan o'chirish uchun kiriting:",
                                cancellationToken: cancellationToken);
                            return;
                        }
                        roomNumber = number;
                    }

                    await _roomService.UpdateRoomAsync(roomId, null, roomNumber, null, null, null, null);
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "✅ Xona raqami muvaffaqiyatli yangilandi!",
                        replyMarkup: KeyboardBuilder.ManageRoomsKeyboard(),
                        cancellationToken: cancellationToken);
                    break;
                case UserState.AdminAddingRoomCapacity:
                    if (!int.TryParse(messageText, out var capacity) || capacity <= 0)
                    {
                        await _botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "❌ Formati noto'g'ri. Sig'im uchun musbat raqam kiriting:",
                            cancellationToken: cancellationToken);
                        return;
                    }
                    await _roomService.UpdateRoomAsync(roomId, null, null, capacity, null, null, null);
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "✅ Xona sig'imi muvaffaqiyatli yangilandi!",
                        replyMarkup: KeyboardBuilder.ManageRoomsKeyboard(),
                        cancellationToken: cancellationToken);
                    break;
                case UserState.AdminAddingRoomDescription:
                    var description = messageText == "-" ? null : messageText;
                    await _roomService.UpdateRoomAsync(roomId, null, null, null, description, null, null);
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "✅ Xona tavsifi muvaffaqiyatli yangilandi!",
                        replyMarkup: KeyboardBuilder.ManageRoomsKeyboard(),
                        cancellationToken: cancellationToken);
                    break;
                case UserState.AdminAddingRoomPhotoUrl:
                    var photoUrl = messageText == "-" ? null : messageText;
                    await _roomService.UpdateRoomAsync(roomId, null, null, null, null, photoUrl, null);
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "✅ Xona rasmi muvaffaqiyatli yangilandi!",
                        replyMarkup: KeyboardBuilder.ManageRoomsKeyboard(),
                        cancellationToken: cancellationToken);
                    break;
            }

            _userStateManager.ClearState(userId);
        }
        catch (Exception ex)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"❌ Xona yangilanishida xatolik: {ex.Message}",
                replyMarkup: KeyboardBuilder.ManageRoomsKeyboard(),
                cancellationToken: cancellationToken);
            _userStateManager.ClearState(userId);
        }
    }
}
