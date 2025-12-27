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
            text: "Управление комнатами:",
            replyMarkup: KeyboardBuilder.ManageRoomsKeyboard(),
            cancellationToken: cancellationToken);
    }

    public async Task StartAddRoomFlow(long chatId, long userId, CancellationToken cancellationToken)
    {
        _userStateManager.SetState(userId, UserState.AdminAddingRoomName);
        _userStateManager.ClearStateData(userId);

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Введите название комнаты:",
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
            text: "Введите номер комнаты (число, или '-' для пропуска):",
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
                text: "❌ Noto'g'ri format номера. Введите положительное число или '-' для пропуска:",
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

    public async Task HandleRoomCapacityInput(long chatId, long userId, string capacityText, CancellationToken cancellationToken)
    {
        if (!int.TryParse(capacityText, out var capacity) || capacity <= 0)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Noto'g'ri format вместимости. Введите положительное число:",
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

    public async Task HandleRoomDescriptionInput(long chatId, long userId, string description, CancellationToken cancellationToken)
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

    public async Task HandleRoomPhotoUrlInput(long chatId, long userId, string photoUrl, CancellationToken cancellationToken)
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

    public async Task ShowRoomList(long chatId, CancellationToken cancellationToken)
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

            // Отправляем заголовок
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "🏠 Список комнат:",
                replyMarkup: KeyboardBuilder.ManageRoomsKeyboard(),
                cancellationToken: cancellationToken);

            // Отправляем каждую комнату отдельно с кнопками
            foreach (var room in rooms.OrderBy(r => r.Number ?? 999).ThenBy(r => r.Name))
            {
                var message = $"🏠 {room.Name}";
                if (room.Number.HasValue)
                    message += $" (№{room.Number})";
                message += $"\n";
                message += $"   Вместимость: {room.Capacity} чел.\n";
                message += $"   Статус: {(room.Status == RoomStatus.Active ? "✅ Активна" : "❌ Неактивна")}\n";
                if (!string.IsNullOrEmpty(room.Description))
                    message += $"   Tavsif: {room.Description}\n";
                message += $"   ID: {room.Id}";

                var buttons = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("✏️ Редактировать", $"edit_room_{room.Id}"),
                        InlineKeyboardButton.WithCallbackData("🗑️ O'chirish", $"delete_room_{room.Id}")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🔄 Статус", $"toggle_room_{room.Id}")
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
                text: $"❌ Ошибка при получении списка комнат: {ex.Message}",
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
                text: $"✏️ Редактирование комнаты '{room.Name}'\n\nТекущие данные:\nNomi: {room.Name}\nНомер: {room.Number?.ToString() ?? "Нет"}\nВместимость: {room.Capacity} чел.\nTavsif: {room.Description ?? "Нет"}\nСтатус: {(room.Status == RoomStatus.Active ? "Активна" : "Неактивна")}\n\nЧто изменить?",
                replyMarkup: new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("📝 Nomi", $"edit_room_name_{roomId}"),
                        InlineKeyboardButton.WithCallbackData("🔢 Номер", $"edit_room_number_{roomId}")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("👥 Вместимость", $"edit_room_capacity_{roomId}"),
                        InlineKeyboardButton.WithCallbackData("📄 Tavsif", $"edit_room_desc_{roomId}")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🔄 Статус", $"toggle_room_{roomId}")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("⬅️ Назад", "admin_list_rooms")
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
                text: $"🗑️ Вы уверены, что хотите удалить комнату '{room.Name}'?\n\n⚠️ Это действие нельзя отменить!\n⚠️ Все активные заказы в этой комнате будут отменены!",
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
                        text: $"✅ Комната '{room.Name}' теперь {(newStatus == RoomStatus.Active ? "активна" : "неактивна")}.",
                        replyMarkup: new InlineKeyboardMarkup(new[]
                        {
                            InlineKeyboardButton.WithCallbackData("⬅️ К списку комнат", "admin_list_rooms")
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
                var roomName = room?.Name ?? "Неизвестная комната";

                var success = await _roomService.DeleteRoomAsync(roomId);

                if (success)
                {
                    await _botClient.EditMessageTextAsync(
                        chatId: chatId,
                        messageId: messageId,
                        text: $"✅ Комната '{roomName}' успешно удалена!",
                        replyMarkup: new InlineKeyboardMarkup(new[]
                        {
                            InlineKeyboardButton.WithCallbackData("⬅️ К списку комнат", "admin_list_rooms")
                        }),
                        cancellationToken: cancellationToken);
                }
                else
                {
                    await _botClient.EditMessageTextAsync(
                        chatId: chatId,
                        messageId: messageId,
                        text: "❌ Не удалось удалить комнату.",
                        replyMarkup: new InlineKeyboardMarkup(new[]
                        {
                            InlineKeyboardButton.WithCallbackData("⬅️ К списку комнат", "admin_list_rooms")
                        }),
                        cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await _botClient.EditMessageTextAsync(
                    chatId: chatId,
                    messageId: messageId,
                    text: $"❌ Ошибка при удалении комнаты: {ex.Message}",
                    replyMarkup: new InlineKeyboardMarkup(new[]
                    {
                        InlineKeyboardButton.WithCallbackData("⬅️ К списку комнат", "admin_list_rooms")
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
            text: "❌ Удаление комнаты отменено.",
            replyMarkup: new InlineKeyboardMarkup(new[]
            {
                InlineKeyboardButton.WithCallbackData("⬅️ К списку комнат", "admin_list_rooms")
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
                        text: "Введите новое название комнаты:",
                        cancellationToken: cancellationToken);
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Введите новое название комнаты:",
                        replyMarkup: new ForceReplyMarkup { Selective = true },
                        cancellationToken: cancellationToken);
                    break;
                case "number":
                    _userStateManager.SetState(userId, UserState.AdminAddingRoomNumber);
                    await _botClient.EditMessageTextAsync(
                        chatId: chatId,
                        messageId: messageId,
                        text: "Введите новый номер комнаты (число или '-' для удаления):",
                        cancellationToken: cancellationToken);
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Введите новый номер комнаты (число или '-' для удаления):",
                        replyMarkup: new ForceReplyMarkup { Selective = true },
                        cancellationToken: cancellationToken);
                    break;
                case "capacity":
                    _userStateManager.SetState(userId, UserState.AdminAddingRoomCapacity);
                    await _botClient.EditMessageTextAsync(
                        chatId: chatId,
                        messageId: messageId,
                        text: "Введите новую вместимость комнаты (число человек):",
                        cancellationToken: cancellationToken);
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Введите новую вместимость комнаты (число человек):",
                        replyMarkup: new ForceReplyMarkup { Selective = true },
                        cancellationToken: cancellationToken);
                    break;
                case "desc":
                    _userStateManager.SetState(userId, UserState.AdminAddingRoomDescription);
                    await _botClient.EditMessageTextAsync(
                        chatId: chatId,
                        messageId: messageId,
                        text: "Введите новое описание комнаты (или '-' для удаления):",
                        cancellationToken: cancellationToken);
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Введите новое описание комнаты (или '-' для удаления):",
                        replyMarkup: new ForceReplyMarkup { Selective = true },
                        cancellationToken: cancellationToken);
                    break;
                default:
                    await _botClient.EditMessageTextAsync(
                        chatId: chatId,
                        messageId: messageId,
                        text: "Неизвестное поле для редактирования.",
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
                        text: "✅ Nomi комнаты успешно обновлено!",
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
                                text: "❌ Noto'g'ri format. Введите число для номера комнаты или '-' для удаления:",
                                cancellationToken: cancellationToken);
                            return;
                        }
                        roomNumber = number;
                    }

                    await _roomService.UpdateRoomAsync(roomId, null, roomNumber, null, null, null, null);
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "✅ Номер комнаты успешно обновлен!",
                        replyMarkup: KeyboardBuilder.ManageRoomsKeyboard(),
                        cancellationToken: cancellationToken);
                    break;
                case UserState.AdminAddingRoomCapacity:
                    if (!int.TryParse(messageText, out var capacity) || capacity <= 0)
                    {
                        await _botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "❌ Noto'g'ri format. Введите положительное число для вместимости:",
                            cancellationToken: cancellationToken);
                        return;
                    }
                    await _roomService.UpdateRoomAsync(roomId, null, null, capacity, null, null, null);
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "✅ Вместимость комнаты успешно обновлена!",
                        replyMarkup: KeyboardBuilder.ManageRoomsKeyboard(),
                        cancellationToken: cancellationToken);
                    break;
                case UserState.AdminAddingRoomDescription:
                    var description = messageText == "-" ? null : messageText;
                    await _roomService.UpdateRoomAsync(roomId, null, null, null, description, null, null);
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "✅ Tavsif комнаты успешно обновлено!",
                        replyMarkup: KeyboardBuilder.ManageRoomsKeyboard(),
                        cancellationToken: cancellationToken);
                    break;
                case UserState.AdminAddingRoomPhotoUrl:
                    var photoUrl = messageText == "-" ? null : messageText;
                    await _roomService.UpdateRoomAsync(roomId, null, null, null, null, photoUrl, null);
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "✅ Фото комнаты успешно обновлено!",
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
                text: $"❌ Ошибка при обновлении комнаты: {ex.Message}",
                replyMarkup: KeyboardBuilder.ManageRoomsKeyboard(),
                cancellationToken: cancellationToken);
            _userStateManager.ClearState(userId);
        }
    }
}
