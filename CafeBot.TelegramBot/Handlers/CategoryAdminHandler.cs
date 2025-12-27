using CafeBot.Application.Services;
using CafeBot.TelegramBot.Keyboards;
using CafeBot.TelegramBot.States;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace CafeBot.TelegramBot.Handlers;

public class CategoryAdminHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly IUserStateManager _userStateManager;
    private readonly IProductService _productService;

    public CategoryAdminHandler(ITelegramBotClient botClient, IUserStateManager userStateManager, IProductService productService)
    {
        _botClient = botClient;
        _userStateManager = userStateManager;
        _productService = productService;
    }

    public async Task ShowManageCategoriesMenu(long chatId, CancellationToken cancellationToken)
    {
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Kategoriyalarni boshqarish:",
            replyMarkup: KeyboardBuilder.ManageCategoriesKeyboard(),
            cancellationToken: cancellationToken);
    }

    public async Task StartAddCategoryFlow(long chatId, long userId, CancellationToken cancellationToken)
    {
        _userStateManager.SetState(userId, UserState.AdminAddingCategoryName);
        _userStateManager.ClearStateData(userId);

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Kategoriya nomini kiriting:",
            replyMarkup: new ForceReplyMarkup { Selective = true },
            cancellationToken: cancellationToken);
    }

    public async Task HandleCategoryNameInput(long chatId, long userId, string categoryName, CancellationToken cancellationToken)
    {
        var stateData = _userStateManager.GetStateData(userId);
        stateData.AdminCategoryName = categoryName;

        _userStateManager.SetState(userId, UserState.AdminAddingCategoryDisplayOrder);
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Ko'rsatish tartibini kiriting (raqam, masalan: 1, 2, 3...):",
            replyMarkup: new ForceReplyMarkup { Selective = true },
            cancellationToken: cancellationToken);
    }

    public async Task HandleCategoryDisplayOrderInput(long chatId, long userId, string displayOrderText, CancellationToken cancellationToken)
    {
        if (!int.TryParse(displayOrderText, out var displayOrder))
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Noto'g'ri format. Ko'rsatish tartibi uchun raqam kiriting:",
                cancellationToken: cancellationToken);
            return;
        }

        var stateData = _userStateManager.GetStateData(userId);
        if (string.IsNullOrEmpty(stateData.AdminCategoryName))
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Xatolik: kategoriya nomi topilmadi. Qaytadan boshlang.",
                replyMarkup: KeyboardBuilder.ManageCategoriesKeyboard(),
                cancellationToken: cancellationToken);
            _userStateManager.ClearState(userId);
            return;
        }

        try
        {
            var category = await _productService.CreateCategoryAsync(stateData.AdminCategoryName, displayOrder);

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"✅ Kategoriya '{category.Name}' muvaffaqiyatli yaratildi!",
                replyMarkup: KeyboardBuilder.ManageCategoriesKeyboard(),
                cancellationToken: cancellationToken);

            _userStateManager.ClearState(userId);
        }
        catch (Exception ex)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"❌ Kategoriya yaratishda xatolik: {ex.Message}",
                replyMarkup: KeyboardBuilder.ManageCategoriesKeyboard(),
                cancellationToken: cancellationToken);
            _userStateManager.ClearState(userId);
        }
    }

    public async Task ShowCategoryList(long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var categories = await _productService.GetActiveCategoriesAsync();

            if (!categories.Any())
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "📁 Hozirda faol kategoriyalar yo'q.",
                    replyMarkup: KeyboardBuilder.ManageCategoriesKeyboard(),
                    cancellationToken: cancellationToken);
                return;
            }

            // Отправляем заголовок
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "📁 Kategoriyalar ro'yxati:",
                replyMarkup: KeyboardBuilder.ManageCategoriesKeyboard(),
                cancellationToken: cancellationToken);

            // Отправляем каждую категорию отдельно с кнопками
            foreach (var category in categories.OrderBy(c => c.DisplayOrder))
            {
                var message = $"📂 {category.Name}\n";
                message += $"   ID: {category.Id}\n";
                message += $"   Tartib: {category.DisplayOrder}\n";
                message += $"   Faol: {(category.IsActive ? "✅ Ha" : "❌ Yo'q")}";

                var buttons = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("✏️ Tahrirlash", $"edit_category_{category.Id}"),
                        InlineKeyboardButton.WithCallbackData("🗑️ O'chirish", $"delete_category_{category.Id}")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🔄 Holat", $"toggle_category_{category.Id}")
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
                text: $"❌ Kategoriyalar ro'yxatini olishda xatolik: {ex.Message}",
                replyMarkup: KeyboardBuilder.ManageCategoriesKeyboard(),
                cancellationToken: cancellationToken);
        }
    }

    public async Task HandleEditCategoryCallback(long userId, long chatId, string callbackData, int messageId, CancellationToken cancellationToken)
    {
        var parts = callbackData.Split('_');
        if (parts.Length == 3 && int.TryParse(parts[2], out var categoryId))
        {
            var category = await _productService.GetCategoryByIdAsync(categoryId);
            if (category == null)
            {
                return;
            }

            // Сохраняем ID категории для редактирования
            var stateData = _userStateManager.GetStateData(userId);
            stateData.AdminEditingCategoryId = categoryId;

            await _botClient.EditMessageTextAsync(
                chatId: chatId,
                messageId: messageId,
                text: $"✏️ Kategoriya '{category.Name}'ni tahrirlash\n\nJoriy ma'lumotlar:\nNomi: {category.Name}\nTartib: {category.DisplayOrder}\nFaol: {(category.IsActive ? "Ha" : "Yo'q")}\n\nNima o'zgartirmoqchisiz?",
                replyMarkup: new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("📝 Nomi", $"edit_category_name_{categoryId}"),
                        InlineKeyboardButton.WithCallbackData("🔢 Tartib", $"edit_category_order_{categoryId}")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🔄 Holat", $"toggle_category_{categoryId}")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("⬅️ Orqaga", "admin_list_categories")
                    }
                }),
                cancellationToken: cancellationToken);
        }
    }

    public async Task HandleDeleteCategoryCallback(long userId, long chatId, string callbackData, int messageId, CancellationToken cancellationToken)
    {
        var parts = callbackData.Split('_');
        if (parts.Length == 3 && int.TryParse(parts[2], out var categoryId))
        {
            var category = await _productService.GetCategoryByIdAsync(categoryId);
            if (category == null)
            {
                return;
            }

            await _botClient.EditMessageTextAsync(
                chatId: chatId,
                messageId: messageId,
                text: $"🗑️ Siz haqiqatan ham '{category.Name}' kategoriyasini o'chirmoqchimisiz?\n\n⚠️ Bu harakatni bekor qilib bo'lmaydi!\n⚠️ Bu kategoriyadagi barcha mahsulotlar ham o'chiriladi!",
                replyMarkup: KeyboardBuilder.YesNoKeyboard("confirm_delete_category", categoryId),
                cancellationToken: cancellationToken);
        }
    }

    public async Task HandleToggleCategoryCallback(long userId, long chatId, string callbackData, int messageId, CancellationToken cancellationToken)
    {
        var parts = callbackData.Split('_');
        if (parts.Length == 3 && int.TryParse(parts[2], out var categoryId))
        {
            try
            {
                var category = await _productService.GetCategoryByIdAsync(categoryId);
                if (category == null)
                {
                    return;
                }

                var newStatus = !category.IsActive;
                var updatedCategory = await _productService.UpdateCategoryAsync(categoryId, null, null, newStatus);

                if (updatedCategory != null)
                {
                    await _botClient.EditMessageTextAsync(
                        chatId: chatId,
                        messageId: messageId,
                        text: $"✅ Kategoriya '{category.Name}' endi {(newStatus ? "faol" : "faol emas")}.",
                        replyMarkup: new InlineKeyboardMarkup(new[]
                        {
                            InlineKeyboardButton.WithCallbackData("⬅️ Kategoriyalar ro'yxatiga", "admin_list_categories")
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

    public async Task HandleConfirmDeleteCategory(long userId, long chatId, string callbackData, int messageId, CancellationToken cancellationToken)
    {
        var parts = callbackData.Split('_');
        if (parts.Length == 5 && int.TryParse(parts[4], out var categoryId))
        {
            try
            {
                var category = await _productService.GetCategoryByIdAsync(categoryId);
                var categoryName = category?.Name ?? "Noma'lum kategoriya";

                var success = await _productService.DeleteCategoryAsync(categoryId);

                if (success)
                {
                    await _botClient.EditMessageTextAsync(
                        chatId: chatId,
                        messageId: messageId,
                        text: $"✅ '{categoryName}' kategoriyasi va barcha mahsulotlari muvaffaqiyatli o'chirildi!",
                        replyMarkup: new InlineKeyboardMarkup(new[]
                        {
                            InlineKeyboardButton.WithCallbackData("⬅️ Kategoriyalar ro'yxatiga", "admin_list_categories")
                        }),
                        cancellationToken: cancellationToken);
                }
                else
                {
                    await _botClient.EditMessageTextAsync(
                        chatId: chatId,
                        messageId: messageId,
                        text: "❌ Kategoriyani o'chirib bo'lmadi.",
                        replyMarkup: new InlineKeyboardMarkup(new[]
                        {
                            InlineKeyboardButton.WithCallbackData("⬅️ Kategoriyalar ro'yxatiga", "admin_list_categories")
                        }),
                        cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await _botClient.EditMessageTextAsync(
                    chatId: chatId,
                    messageId: messageId,
                    text: $"❌ Kategoriyani o'chirishda xatolik: {ex.Message}",
                    replyMarkup: new InlineKeyboardMarkup(new[]
                    {
                        InlineKeyboardButton.WithCallbackData("⬅️ Kategoriyalar ro'yxatiga", "admin_list_categories")
                    }),
                    cancellationToken: cancellationToken);
            }
        }
    }

    public async Task HandleCancelDeleteCategory(long userId, long chatId, string callbackData, int messageId, CancellationToken cancellationToken)
    {
        await _botClient.EditMessageTextAsync(
            chatId: chatId,
            messageId: messageId,
            text: "❌ Kategoriyani o'chirish bekor qilindi.",
            replyMarkup: new InlineKeyboardMarkup(new[]
            {
                InlineKeyboardButton.WithCallbackData("⬅️ Kategoriyalar ro'yxatiga", "admin_list_categories")
            }),
            cancellationToken: cancellationToken);
    }

    public async Task HandleCategoryEditField(string callbackQueryId, long userId, long chatId, string callbackData, int messageId, CancellationToken cancellationToken)
    {
        // Ответ на callback
        await _botClient.AnswerCallbackQueryAsync(callbackQueryId, cancellationToken: cancellationToken);

        var parts = callbackData.Split('_');
        if (parts.Length == 4 && int.TryParse(parts[3], out var categoryId))
        {
            var field = parts[2]; // name, order
            var stateData = _userStateManager.GetStateData(userId);

            // Сохраняем ID редактируемой категории
            stateData.AdminEditingCategoryId = categoryId;

            switch (field)
            {
                case "name":
                    _userStateManager.SetState(userId, UserState.AdminAddingCategoryName);
                    await _botClient.EditMessageTextAsync(
                        chatId: chatId,
                        messageId: messageId,
                        text: "Kategoriya uchun yangi nom kiriting:",
                        cancellationToken: cancellationToken);
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Kategoriya uchun yangi nom kiriting:",
                        replyMarkup: new ForceReplyMarkup { Selective = true },
                        cancellationToken: cancellationToken);
                    break;
                case "order":
                    _userStateManager.SetState(userId, UserState.AdminAddingCategoryDisplayOrder);
                    await _botClient.EditMessageTextAsync(
                        chatId: chatId,
                        messageId: messageId,
                        text: "Yangi ko'rsatish tartibini kiriting (raqam):",
                        cancellationToken: cancellationToken);
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Yangi ko'rsatish tartibini kiriting (raqam):",
                        replyMarkup: new ForceReplyMarkup { Selective = true },
                        cancellationToken: cancellationToken);
                    break;
                default:
                    await _botClient.EditMessageTextAsync(
                        chatId: chatId,
                        messageId: messageId,
                        text: "Tahrirlash uchun noma'lum maydon.",
                        replyMarkup: KeyboardBuilder.ManageCategoriesKeyboard(),
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
            case "admin_manage_categories":
                await ShowManageCategoriesMenu(chatId, cancellationToken);
                break;
            case "admin_add_category":
                await StartAddCategoryFlow(chatId, userId, cancellationToken);
                break;
            case "admin_list_categories":
                await ShowCategoryList(chatId, cancellationToken);
                break;
            default:
                if (data.StartsWith("delete_category_"))
                {
                    await HandleDeleteCategoryCallback(userId, chatId, data, callbackQuery.Message.MessageId, cancellationToken);
                }
                else if (data.StartsWith("toggle_category_"))
                {
                    await HandleToggleCategoryCallback(userId, chatId, data, callbackQuery.Message.MessageId, cancellationToken);
                }
                else if (data.StartsWith("confirm_delete_category_yes_"))
                {
                    await HandleConfirmDeleteCategory(userId, chatId, data, callbackQuery.Message.MessageId, cancellationToken);
                }
                else if (data.StartsWith("confirm_delete_category_no_"))
                {
                    await HandleCancelDeleteCategory(userId, chatId, data, callbackQuery.Message.MessageId, cancellationToken);
                }
                else if (data.StartsWith("edit_category_"))
                {
                    var parts = data.Split('_');
                    if (parts.Length == 3) // edit_category_{id} - показ меню редактирования
                    {
                        await HandleEditCategoryCallback(userId, chatId, data, callbackQuery.Message.MessageId, cancellationToken);
                    }
                    else // edit_category_{field}_{id} - редактирование конкретного поля
                    {
                        await HandleCategoryEditField(callbackQuery.Id, userId, chatId, data, callbackQuery.Message.MessageId, cancellationToken);
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

        // Если есть AdminEditingCategoryId, значит это редактирование существующей категории
        if (stateData.AdminEditingCategoryId.HasValue)
        {
            await HandleCategoryEditInput(chatId, userId, currentState, messageText, cancellationToken);
        }
        else
        {
            // Обычное создание категории
            switch (currentState)
            {
                case UserState.AdminAddingCategoryName:
                    await HandleCategoryNameInput(chatId, userId, messageText, cancellationToken);
                    break;
                case UserState.AdminAddingCategoryDisplayOrder:
                    await HandleCategoryDisplayOrderInput(chatId, userId, messageText, cancellationToken);
                    break;
            }
        }
    }

    private async Task HandleCategoryEditInput(long chatId, long userId, UserState currentState, string messageText, CancellationToken cancellationToken)
    {
        var stateData = _userStateManager.GetStateData(userId);
        var categoryId = stateData.AdminEditingCategoryId.Value;

        try
        {
            switch (currentState)
            {
                case UserState.AdminAddingCategoryName:
                    await _productService.UpdateCategoryAsync(categoryId, messageText, null, null);
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "✅ Kategoriya nomi muvaffaqiyatli yangilandi!",
                        replyMarkup: KeyboardBuilder.ManageCategoriesKeyboard(),
                        cancellationToken: cancellationToken);
                    break;
                case UserState.AdminAddingCategoryDisplayOrder:
                    if (!int.TryParse(messageText, out var displayOrder))
                    {
                        await _botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "❌ Formati noto'g'ri. Ko'rsatish tartibi uchun raqam kiriting:",
                            cancellationToken: cancellationToken);
                        return;
                    }
                    await _productService.UpdateCategoryAsync(categoryId, null, displayOrder, null);
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "✅ Kategoriya ko'rsatish tartibi muvaffaqiyatli yangilandi!",
                        replyMarkup: KeyboardBuilder.ManageCategoriesKeyboard(),
                        cancellationToken: cancellationToken);
                    break;
            }

            _userStateManager.ClearState(userId);
        }
        catch (Exception ex)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"❌ Kategoriya yangilanishida xatolik: {ex.Message}",
                replyMarkup: KeyboardBuilder.ManageCategoriesKeyboard(),
                cancellationToken: cancellationToken);
            _userStateManager.ClearState(userId);
        }
    }
}
