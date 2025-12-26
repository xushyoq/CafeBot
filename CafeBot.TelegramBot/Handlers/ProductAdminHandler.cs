using CafeBot.Application.Services;
using CafeBot.Core.Enums;
using CafeBot.TelegramBot.Keyboards;
using CafeBot.TelegramBot.States;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace CafeBot.TelegramBot.Handlers;

public class ProductAdminHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly IUserStateManager _userStateManager;
    private readonly IProductService _productService;

    public ProductAdminHandler(ITelegramBotClient botClient, IUserStateManager userStateManager, IProductService productService)
    {
        _botClient = botClient;
        _userStateManager = userStateManager;
        _productService = productService;
    }

    public async Task ShowManageProductsMenu(long chatId, CancellationToken cancellationToken)
    {
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Управление продуктами:",
            replyMarkup: KeyboardBuilder.ManageProductsKeyboard(),
            cancellationToken: cancellationToken);
    }

    public async Task StartAddProductFlow(long chatId, long userId, CancellationToken cancellationToken)
    {
        _userStateManager.SetState(userId, UserState.AdminAddingProductCategory);
        _userStateManager.ClearStateData(userId);

        var categories = await _productService.GetActiveCategoriesAsync();
        if (!categories.Any())
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Нет активных категорий. Сначала создайте категорию.",
                replyMarkup: KeyboardBuilder.ManageProductsKeyboard(),
                cancellationToken: cancellationToken);
            _userStateManager.ClearState(userId);
            return;
        }

        var categoryList = "Доступные категории:\n\n";
        foreach (var category in categories)
        {
            categoryList += $"{category.Id}. {category.Name}\n";
        }
        categoryList += "\nВведите ID категории для продукта:";

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: categoryList,
            replyMarkup: new ForceReplyMarkup { Selective = true },
            cancellationToken: cancellationToken);
    }

    public async Task HandleProductCategoryInput(long chatId, long userId, string categoryIdText, CancellationToken cancellationToken)
    {
        if (!int.TryParse(categoryIdText, out var categoryId))
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Неверный формат. Введите ID категории (число):",
                cancellationToken: cancellationToken);
            return;
        }

        var category = await _productService.GetCategoryByIdAsync(categoryId);
        if (category == null)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Категория с таким ID не найдена. Попробуйте еще раз:",
                cancellationToken: cancellationToken);
            return;
        }

        var stateData = _userStateManager.GetStateData(userId);
        stateData.AdminProductCategoryId = categoryId;

        _userStateManager.SetState(userId, UserState.AdminAddingProductName);
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Введите название продукта:",
            replyMarkup: new ForceReplyMarkup { Selective = true },
            cancellationToken: cancellationToken);
    }

    public async Task HandleProductNameInput(long chatId, long userId, string productName, CancellationToken cancellationToken)
    {
        var stateData = _userStateManager.GetStateData(userId);
        stateData.AdminProductName = productName;

        _userStateManager.SetState(userId, UserState.AdminAddingProductDescription);
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Введите описание продукта (или '-' для пропуска):",
            replyMarkup: new ForceReplyMarkup { Selective = true },
            cancellationToken: cancellationToken);
    }

    public async Task HandleProductDescriptionInput(long chatId, long userId, string description, CancellationToken cancellationToken)
    {
        var stateData = _userStateManager.GetStateData(userId);
        stateData.AdminProductDescription = description == "-" ? null : description;

        _userStateManager.SetState(userId, UserState.AdminAddingProductPrice);
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Введите цену продукта (число, например: 15000, 25000):",
            replyMarkup: new ForceReplyMarkup { Selective = true },
            cancellationToken: cancellationToken);
    }

    public async Task HandleProductPriceInput(long chatId, long userId, string priceText, CancellationToken cancellationToken)
    {
        if (!decimal.TryParse(priceText, out var price) || price <= 0)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Неверный формат цены. Введите положительное число:",
                cancellationToken: cancellationToken);
            return;
        }

        var stateData = _userStateManager.GetStateData(userId);
        stateData.AdminProductPrice = price;

        _userStateManager.SetState(userId, UserState.AdminAddingProductUnit);
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Выберите единицу измерения:",
            replyMarkup: KeyboardBuilder.ProductUnitKeyboard(),
            cancellationToken: cancellationToken);
    }

    public async Task HandleSetProductUnitCallback(long userId, long chatId, string callbackData, int messageId, CancellationToken cancellationToken)
    {
        var parts = callbackData.Split('_');
        if (parts.Length == 4 && Enum.TryParse<ProductUnit>(parts[3], out var unit))
        {
            var stateData = _userStateManager.GetStateData(userId);

            // Если это редактирование существующего продукта
            if (stateData.AdminEditingProductId.HasValue)
            {
                try
                {
                    await _productService.UpdateProductAsync(stateData.AdminEditingProductId.Value, null, null, null, null, unit, null, null, null);
                    await _botClient.EditMessageTextAsync(
                        chatId: chatId,
                        messageId: messageId,
                        text: "✅ Единица измерения продукта успешно обновлена!",
                        replyMarkup: KeyboardBuilder.ManageProductsKeyboard(),
                        cancellationToken: cancellationToken);
                    _userStateManager.ClearState(userId);
                }
                catch (Exception ex)
                {
                    await _botClient.EditMessageTextAsync(
                        chatId: chatId,
                        messageId: messageId,
                        text: $"❌ Ошибка при обновлении единицы измерения: {ex.Message}",
                        replyMarkup: KeyboardBuilder.ManageProductsKeyboard(),
                        cancellationToken: cancellationToken);
                    _userStateManager.ClearState(userId);
                }
            }
            else
            {
                // Это создание нового продукта
                stateData.AdminProductUnit = unit;
                _userStateManager.SetState(userId, UserState.AdminAddingProductPhotoUrl);
                await _botClient.EditMessageTextAsync(
                    chatId: chatId,
                    messageId: messageId,
                    text: "Введите URL фото продукта (или '-' для пропуска):",
                    cancellationToken: cancellationToken);

                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Введите URL фото продукта (или '-' для пропуска):",
                    replyMarkup: new ForceReplyMarkup { Selective = true },
                    cancellationToken: cancellationToken);
            }
        }
    }

    public async Task HandleProductPhotoUrlInput(long chatId, long userId, string photoUrl, CancellationToken cancellationToken)
    {
        var stateData = _userStateManager.GetStateData(userId);
        stateData.AdminProductPhotoUrl = photoUrl == "-" ? null : photoUrl;

        _userStateManager.SetState(userId, UserState.AdminAddingProductDisplayOrder);
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Введите порядок отображения (число, например: 1, 2, 3...):",
            replyMarkup: new ForceReplyMarkup { Selective = true },
            cancellationToken: cancellationToken);
    }

    public async Task HandleProductDisplayOrderInput(long chatId, long userId, string displayOrderText, CancellationToken cancellationToken)
    {
        if (!int.TryParse(displayOrderText, out var displayOrder))
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Неверный формат. Введите число для порядка отображения:",
                cancellationToken: cancellationToken);
            return;
        }

        var stateData = _userStateManager.GetStateData(userId);

        // Проверяем, что все обязательные поля заполнены
        if (!stateData.AdminProductCategoryId.HasValue ||
            string.IsNullOrEmpty(stateData.AdminProductName) ||
            !stateData.AdminProductPrice.HasValue ||
            !stateData.AdminProductUnit.HasValue)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Ошибка: не все данные заполнены. Начните заново.",
                replyMarkup: KeyboardBuilder.ManageProductsKeyboard(),
                cancellationToken: cancellationToken);
            _userStateManager.ClearState(userId);
            return;
        }

        try
        {
            var product = await _productService.CreateProductAsync(
                stateData.AdminProductCategoryId.Value,
                stateData.AdminProductName,
                stateData.AdminProductDescription,
                stateData.AdminProductPrice.Value,
                stateData.AdminProductUnit.Value,
                stateData.AdminProductPhotoUrl,
                displayOrder);

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"✅ Продукт '{product.Name}' успешно создан!",
                replyMarkup: KeyboardBuilder.ManageProductsKeyboard(),
                cancellationToken: cancellationToken);

            _userStateManager.ClearState(userId);
        }
        catch (Exception ex)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"❌ Ошибка при создании продукта: {ex.Message}",
                replyMarkup: KeyboardBuilder.ManageProductsKeyboard(),
                cancellationToken: cancellationToken);
            _userStateManager.ClearState(userId);
        }
    }

    public async Task ShowProductList(long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var products = await _productService.GetAllProductsForAdminAsync();
            var categories = await _productService.GetActiveCategoriesAsync();

            if (!products.Any())
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "📦 В данный момент нет доступных продуктов.",
                    replyMarkup: KeyboardBuilder.ManageProductsKeyboard(),
                    cancellationToken: cancellationToken);
                return;
            }

            var categoryDict = categories.ToDictionary(c => c.Id, c => c.Name);

            // Отправляем заголовок
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "📦 Список продуктов:",
                replyMarkup: KeyboardBuilder.ManageProductsKeyboard(),
                cancellationToken: cancellationToken);

            // Отправляем каждый продукт отдельно с кнопками
            foreach (var product in products.OrderBy(p => p.CategoryId).ThenBy(p => p.DisplayOrder))
            {
                var categoryName = categoryDict.ContainsKey(product.CategoryId) ? categoryDict[product.CategoryId] : "Неизвестная категория";
                var message = $"🛒 {product.Name}\n";
                message += $"   Категория: {categoryName}\n";
                message += $"   Цена: {product.Price:N0} сум\n";
                message += $"   Единица: {product.Unit}\n";
                if (!string.IsNullOrEmpty(product.Description))
                    message += $"   Описание: {product.Description}\n";
                message += $"   Доступен: {(product.IsAvailable ? "✅ Да" : "❌ Нет")}\n";
                message += $"   ID: {product.Id}";

                var buttons = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("✏️ Редактировать", $"edit_product_{product.Id}"),
                        InlineKeyboardButton.WithCallbackData("🗑️ Удалить", $"delete_product_{product.Id}")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🔄 Доступность", $"toggle_product_{product.Id}")
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
                text: $"❌ Ошибка при получении списка продуктов: {ex.Message}",
                replyMarkup: KeyboardBuilder.ManageProductsKeyboard(),
                cancellationToken: cancellationToken);
        }
    }

    public async Task HandleEditProductCallback(long userId, long chatId, string callbackData, int messageId, CancellationToken cancellationToken)
    {
        var parts = callbackData.Split('_');
        if (parts.Length == 3 && int.TryParse(parts[2], out var productId))
        {
            var product = await _productService.GetProductWithCategoryAsync(productId);
            if (product == null)
            {
                return;
            }

            // Сохраняем ID продукта для редактирования
            var stateData = _userStateManager.GetStateData(userId);
            stateData.AdminEditingProductId = productId;

            await _botClient.EditMessageTextAsync(
                chatId: chatId,
                messageId: messageId,
                text: $"✏️ Редактирование продукта '{product.Name}'\n\nТекущие данные:\nКатегория: {product.Category?.Name ?? "Неизвестная"}\nЦена: {product.Price:N0} сум\nЕдиница: {product.Unit}\nОписание: {product.Description ?? "Нет"}\nПорядок: {product.DisplayOrder}\nДоступен: {(product.IsAvailable ? "Да" : "Нет")}\n\nЧто изменить?",
                replyMarkup: new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("📝 Название", $"edit_product_name_{productId}"),
                        InlineKeyboardButton.WithCallbackData("💰 Цена", $"edit_product_price_{productId}")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("📏 Единица", $"edit_product_unit_{productId}"),
                        InlineKeyboardButton.WithCallbackData("📄 Описание", $"edit_product_desc_{productId}")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🔢 Порядок", $"edit_product_order_{productId}"),
                        InlineKeyboardButton.WithCallbackData("🔄 Доступность", $"toggle_product_{productId}")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("⬅️ Назад", "admin_list_products")
                    }
                }),
                cancellationToken: cancellationToken);
        }
    }

    public async Task HandleDeleteProductCallback(long userId, long chatId, string callbackData, int messageId, CancellationToken cancellationToken)
    {
        var parts = callbackData.Split('_');
        if (parts.Length == 3 && int.TryParse(parts[2], out var productId))
        {
            var product = await _productService.GetProductByIdAsync(productId);
            if (product == null)
            {
                return;
            }

            await _botClient.EditMessageTextAsync(
                chatId: chatId,
                messageId: messageId,
                text: $"🗑️ Вы уверены, что хотите удалить продукт '{product.Name}'?\n\n⚠️ Это действие нельзя отменить!",
                replyMarkup: KeyboardBuilder.YesNoKeyboard("confirm_delete_product", productId),
                cancellationToken: cancellationToken);
        }
    }

    public async Task HandleToggleProductCallback(long userId, long chatId, string callbackData, int messageId, CancellationToken cancellationToken)
    {
        var parts = callbackData.Split('_');
        if (parts.Length == 3 && int.TryParse(parts[2], out var productId))
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(productId);
                if (product == null)
                {
                    return;
                }

                var success = await _productService.ToggleProductAvailabilityAsync(productId);

                if (success)
                {
                    var newStatus = !product.IsAvailable;
                    await _botClient.EditMessageTextAsync(
                        chatId: chatId,
                        messageId: messageId,
                        text: $"✅ Продукт '{product.Name}' теперь {(newStatus ? "доступен" : "недоступен")}.",
                        replyMarkup: new InlineKeyboardMarkup(new[]
                        {
                            InlineKeyboardButton.WithCallbackData("⬅️ К списку продуктов", "admin_list_products")
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

    public async Task HandleConfirmDeleteProduct(long userId, long chatId, string callbackData, int messageId, CancellationToken cancellationToken)
    {
        var parts = callbackData.Split('_');
        if (parts.Length == 5 && int.TryParse(parts[4], out var productId))
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(productId);
                var productName = product?.Name ?? "Неизвестный продукт";

                var success = await _productService.DeleteProductAsync(productId);

                if (success)
                {
                    await _botClient.EditMessageTextAsync(
                        chatId: chatId,
                        messageId: messageId,
                        text: $"✅ Продукт '{productName}' успешно удален!",
                        replyMarkup: new InlineKeyboardMarkup(new[]
                        {
                            InlineKeyboardButton.WithCallbackData("⬅️ К списку продуктов", "admin_list_products")
                        }),
                        cancellationToken: cancellationToken);
                }
                else
                {
                    await _botClient.EditMessageTextAsync(
                        chatId: chatId,
                        messageId: messageId,
                        text: "❌ Не удалось удалить продукт.",
                        replyMarkup: new InlineKeyboardMarkup(new[]
                        {
                            InlineKeyboardButton.WithCallbackData("⬅️ К списку продуктов", "admin_list_products")
                        }),
                        cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await _botClient.EditMessageTextAsync(
                    chatId: chatId,
                    messageId: messageId,
                    text: $"❌ Ошибка при удалении продукта: {ex.Message}",
                    replyMarkup: new InlineKeyboardMarkup(new[]
                    {
                        InlineKeyboardButton.WithCallbackData("⬅️ К списку продуктов", "admin_list_products")
                    }),
                    cancellationToken: cancellationToken);
            }
        }
    }

    public async Task HandleCancelDeleteProduct(long userId, long chatId, string callbackData, int messageId, CancellationToken cancellationToken)
    {
        await _botClient.EditMessageTextAsync(
            chatId: chatId,
            messageId: messageId,
            text: "❌ Удаление продукта отменено.",
            replyMarkup: new InlineKeyboardMarkup(new[]
            {
                InlineKeyboardButton.WithCallbackData("⬅️ К списку продуктов", "admin_list_products")
            }),
            cancellationToken: cancellationToken);
    }

    public async Task HandleProductEditField(string callbackQueryId, long userId, long chatId, string callbackData, int messageId, CancellationToken cancellationToken)
    {
        // Ответ на callback
        await _botClient.AnswerCallbackQueryAsync(callbackQueryId, cancellationToken: cancellationToken);

        var parts = callbackData.Split('_');
        if (parts.Length == 4 && int.TryParse(parts[3], out var productId))
        {
            var field = parts[2]; // name, price, unit, desc, order (для edit_product_{field}_{id})
            var stateData = _userStateManager.GetStateData(userId);

            // Сохраняем ID редактируемого продукта
            stateData.AdminEditingProductId = productId;

            switch (field)
            {
                case "name":
                    _userStateManager.SetState(userId, UserState.AdminAddingProductName);
                    await _botClient.EditMessageTextAsync(
                        chatId: chatId,
                        messageId: messageId,
                        text: "Введите новое название продукта:",
                        cancellationToken: cancellationToken);
                    break;
                case "price":
                    _userStateManager.SetState(userId, UserState.AdminAddingProductPrice);
                    await _botClient.EditMessageTextAsync(
                        chatId: chatId,
                        messageId: messageId,
                        text: "Введите новую цену продукта (число):",
                        cancellationToken: cancellationToken);
                    break;
                case "unit":
                    await _botClient.EditMessageTextAsync(
                        chatId: chatId,
                        messageId: messageId,
                        text: "Выберите новую единицу измерения:",
                        replyMarkup: KeyboardBuilder.ProductUnitKeyboard(),
                        cancellationToken: cancellationToken);
                    break;
                case "desc":
                    _userStateManager.SetState(userId, UserState.AdminAddingProductDescription);
                    await _botClient.EditMessageTextAsync(
                        chatId: chatId,
                        messageId: messageId,
                        text: "Введите новое описание продукта (или '-' для удаления):",
                        cancellationToken: cancellationToken);
                    break;
                case "order":
                    _userStateManager.SetState(userId, UserState.AdminAddingProductDisplayOrder);
                    await _botClient.EditMessageTextAsync(
                        chatId: chatId,
                        messageId: messageId,
                        text: "Введите новый порядок отображения (число):",
                        cancellationToken: cancellationToken);
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
            case "admin_manage_products":
                await ShowManageProductsMenu(chatId, cancellationToken);
                break;
            case "admin_add_product":
                await StartAddProductFlow(chatId, userId, cancellationToken);
                break;
            case "admin_list_products":
                await ShowProductList(chatId, cancellationToken);
                break;
            default:
                if (data.StartsWith("set_product_unit_"))
                {
                    await HandleSetProductUnitCallback(userId, chatId, data, callbackQuery.Message.MessageId, cancellationToken);
                }
                else if (data.StartsWith("delete_product_"))
                {
                    await HandleDeleteProductCallback(userId, chatId, data, callbackQuery.Message.MessageId, cancellationToken);
                }
                else if (data.StartsWith("toggle_product_"))
                {
                    await HandleToggleProductCallback(userId, chatId, data, callbackQuery.Message.MessageId, cancellationToken);
                }
                else if (data.StartsWith("confirm_delete_product_yes_"))
                {
                    await HandleConfirmDeleteProduct(userId, chatId, data, callbackQuery.Message.MessageId, cancellationToken);
                }
                else if (data.StartsWith("confirm_delete_product_no_"))
                {
                    await HandleCancelDeleteProduct(userId, chatId, data, callbackQuery.Message.MessageId, cancellationToken);
                }
                else if (data.StartsWith("edit_product_"))
                {
                    var parts = data.Split('_');
                    if (parts.Length == 3) // edit_product_{id} - показ меню редактирования
                    {
                        await HandleEditProductCallback(userId, chatId, data, callbackQuery.Message.MessageId, cancellationToken);
                    }
                    else // edit_product_{field}_{id} - редактирование конкретного поля
                    {
                        await HandleProductEditField(callbackQuery.Id, userId, chatId, data, callbackQuery.Message.MessageId, cancellationToken);
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

        // Если есть AdminEditingProductId, значит это редактирование существующего продукта
        if (stateData.AdminEditingProductId.HasValue)
        {
            await HandleProductEditInput(chatId, userId, currentState, messageText, cancellationToken);
        }
        else
        {
            // Обычное создание продукта
            switch (currentState)
            {
                case UserState.AdminAddingProductCategory:
                    await HandleProductCategoryInput(chatId, userId, messageText, cancellationToken);
                    break;
                case UserState.AdminAddingProductName:
                    await HandleProductNameInput(chatId, userId, messageText, cancellationToken);
                    break;
                case UserState.AdminAddingProductDescription:
                    await HandleProductDescriptionInput(chatId, userId, messageText, cancellationToken);
                    break;
                case UserState.AdminAddingProductPrice:
                    await HandleProductPriceInput(chatId, userId, messageText, cancellationToken);
                    break;
                case UserState.AdminAddingProductPhotoUrl:
                    await HandleProductPhotoUrlInput(chatId, userId, messageText, cancellationToken);
                    break;
                case UserState.AdminAddingProductDisplayOrder:
                    await HandleProductDisplayOrderInput(chatId, userId, messageText, cancellationToken);
                    break;
                default:
                    await _botClient.SendTextMessageAsync(chatId, "Неизвестная команда. Пожалуйста, используйте кнопки.", replyMarkup: KeyboardBuilder.AdminMainMenuKeyboard(), cancellationToken: cancellationToken);
                    break;
            }
        }
    }

    private async Task HandleProductEditInput(long chatId, long userId, UserState currentState, string messageText, CancellationToken cancellationToken)
    {
        var stateData = _userStateManager.GetStateData(userId);
        var productId = stateData.AdminEditingProductId.Value;


        try
        {
            switch (currentState)
            {
                case UserState.AdminAddingProductName:
                    await _productService.UpdateProductAsync(productId, null, messageText, null, null, null, null, null, null);
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "✅ Название продукта успешно обновлено!",
                        replyMarkup: KeyboardBuilder.ManageProductsKeyboard(),
                        cancellationToken: cancellationToken);
                    break;
                case UserState.AdminAddingProductDescription:
                    var description = messageText == "-" ? null : messageText;
                    await _productService.UpdateProductAsync(productId, null, null, description, null, null, null, null, null);
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "✅ Описание продукта успешно обновлено!",
                        replyMarkup: KeyboardBuilder.ManageProductsKeyboard(),
                        cancellationToken: cancellationToken);
                    break;
                case UserState.AdminAddingProductPrice:
                    if (!decimal.TryParse(messageText, out var price) || price <= 0)
                    {
                        await _botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "❌ Неверный формат цены. Введите положительное число:",
                            cancellationToken: cancellationToken);
                        return;
                    }
                    await _productService.UpdateProductAsync(productId, null, null, null, price, null, null, null, null);
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "✅ Цена продукта успешно обновлена!",
                        replyMarkup: KeyboardBuilder.ManageProductsKeyboard(),
                        cancellationToken: cancellationToken);
                    break;
                case UserState.AdminAddingProductDisplayOrder:
                    if (!int.TryParse(messageText, out var displayOrder))
                    {
                        await _botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "❌ Неверный формат. Введите число для порядка отображения:",
                            cancellationToken: cancellationToken);
                        return;
                    }
                    await _productService.UpdateProductAsync(productId, null, null, null, null, null, null, displayOrder, null);
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "✅ Порядок отображения продукта успешно обновлен!",
                        replyMarkup: KeyboardBuilder.ManageProductsKeyboard(),
                        cancellationToken: cancellationToken);
                    break;
                default:
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: $"❌ Неизвестное состояние редактирования: {currentState}",
                        replyMarkup: KeyboardBuilder.ManageProductsKeyboard(),
                        cancellationToken: cancellationToken);
                    break;
            }

            _userStateManager.ClearState(userId);
        }
        catch (Exception ex)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"❌ Ошибка при обновлении продукта: {ex.Message}",
                replyMarkup: KeyboardBuilder.ManageProductsKeyboard(),
                cancellationToken: cancellationToken);
            _userStateManager.ClearState(userId);
        }
    }
}
