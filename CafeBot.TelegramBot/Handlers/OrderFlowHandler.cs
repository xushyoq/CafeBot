using CafeBot.Application.Services;
using CafeBot.Core.Enums;
using CafeBot.Core.Interfaces; // ← ДОБАВЬТЕ
using CafeBot.TelegramBot.Keyboards;
using CafeBot.TelegramBot.States;
using Microsoft.Extensions.DependencyInjection; // ← ДОБАВЬТЕ
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace CafeBot.TelegramBot.Handlers;

public class OrderFlowHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly IOrderService _orderService;
    private readonly IRoomService _roomService;
    private readonly IProductService _productService;
    private readonly IUserStateManager _stateManager;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrderFlowHandler> _logger;

    public OrderFlowHandler(
        ITelegramBotClient botClient,
        IOrderService orderService,
        IRoomService roomService,
        IProductService productService,
        IUserStateManager stateManager,
        IServiceProvider serviceProvider,
        ILogger<OrderFlowHandler> logger)
    {
        _botClient = botClient;
        _orderService = orderService;
        _roomService = roomService;
        _productService = productService;
        _stateManager = stateManager;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartOrderCreationAsync(long chatId, long userId, CancellationToken cancellationToken)
    {
        _stateManager.SetState(userId, UserState.SelectingDate);
        
        var data = _stateManager.GetData(userId);
        data.Clear();

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "📅 Выберите дату бронирования:",
            replyMarkup: KeyboardBuilder.DateSelectionKeyboard(),
            cancellationToken: cancellationToken
        );
    }

    public async Task HandleCallbackAsync(CallbackQuery callbackQuery, long userId, CancellationToken cancellationToken)
    {
        var chatId = callbackQuery.Message!.Chat.Id;
        var data = callbackQuery.Data ?? string.Empty;

        _logger.LogInformation("Callback received: {Data} from user {UserId}", data, userId);

        // Обработка отмены
        if (data == "cancel")
        {
            await CancelOrderCreationAsync(chatId, userId, cancellationToken);
            await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
            return;
        }

        // Добавьте эту обработку для finish_adding
        if (data == "finish_adding")
        {
            await FinishOrderCreationAsync(chatId, userId, cancellationToken);
            await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
            return;
        }

        var currentState = _stateManager.GetState(userId);

        try
        {
            switch (currentState)
            {
                case UserState.SelectingDate:
                    await HandleDateSelectionAsync(chatId, userId, data, cancellationToken);
                    break;

                case UserState.SelectingTimeSlot:
                    await HandleTimeSlotSelectionAsync(chatId, userId, data, cancellationToken);
                    break;

                case UserState.SelectingRoom:
                    await HandleRoomSelectionAsync(chatId, userId, data, cancellationToken);
                    break;

                case UserState.SelectingCategory:
                    await HandleCategorySelectionAsync(chatId, userId, data, cancellationToken);
                    break;

                case UserState.SelectingProduct:
                    await HandleProductSelectionAsync(chatId, userId, data, cancellationToken);
                    break;

                default:
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "❓ Неизвестное действие. Начните заново с /start",
                        cancellationToken: cancellationToken
                    );
                    break;
            }

            await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling callback");
            await _botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                text: "❌ Произошла ошибка. Попробуйте еще раз.",
                showAlert: true,
                cancellationToken: cancellationToken
            );
        }
    }

    private async Task HandleDateSelectionAsync(long chatId, long userId, string data, CancellationToken cancellationToken)
{
    if (!data.StartsWith("date_"))
        return;

    var dateStr = data.Replace("date_", "");
    if (!DateTime.TryParse(dateStr, out var selectedDate))
    {
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "❌ Неверный формат даты. Попробуйте еще раз.",
            cancellationToken: cancellationToken
        );
        return;
    }

    // ВАЖНО: Конвертируем в UTC для PostgreSQL
    selectedDate = DateTime.SpecifyKind(selectedDate, DateTimeKind.Utc);

    var stateData = _stateManager.GetData(userId);
    stateData.SelectedDate = selectedDate;

    _stateManager.SetState(userId, UserState.SelectingTimeSlot);

    await _botClient.SendTextMessageAsync(
        chatId: chatId,
        text: $"✅ Дата: {selectedDate:dd.MM.yyyy}\n\n⏰ Выберите время:",
        replyMarkup: KeyboardBuilder.TimeSlotSelectionKeyboard(),
        cancellationToken: cancellationToken
    );
}

    private async Task HandleTimeSlotSelectionAsync(long chatId, long userId, string data, CancellationToken cancellationToken)
    {
        if (data == "back_to_date")
        {
            _stateManager.SetState(userId, UserState.SelectingDate);
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "📅 Выберите дату бронирования:",
                replyMarkup: KeyboardBuilder.DateSelectionKeyboard(),
                cancellationToken: cancellationToken
            );
            return;
        }

        TimeSlot timeSlot;
        string timeSlotText;

        if (data == "timeslot_day")
        {
            timeSlot = TimeSlot.Day;
            timeSlotText = "День (12:00-16:00)";
        }
        else if (data == "timeslot_evening")
        {
            timeSlot = TimeSlot.Evening;
            timeSlotText = "Вечер (17:00-22:00)";
        }
        else
        {
            return;
        }

        var stateData = _stateManager.GetData(userId);
        stateData.SelectedTimeSlot = timeSlot;

        _stateManager.SetState(userId, UserState.SelectingRoom);

        // Получаем доступные комнаты
        var availableRooms = await _roomService.GetAvailableRoomsAsync(
            stateData.SelectedDate!.Value,
            timeSlot
        );

        if (!availableRooms.Any())
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ К сожалению, все комнаты заняты на это время.\n\nПопробуйте выбрать другую дату или время.",
                replyMarkup: KeyboardBuilder.DateSelectionKeyboard(),
                cancellationToken: cancellationToken
            );
            _stateManager.SetState(userId, UserState.SelectingDate);
            return;
        }

        // Создаем клавиатуру с комнатами
        var buttons = availableRooms.Select(room =>
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    $"🏠 {room.Name} (до {room.Capacity} чел.)",
                    $"room_{room.Id}"
                )
            }
        ).ToList();

        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData("⬅️ Назад", "back_to_date")
        });

        var keyboard = new InlineKeyboardMarkup(buttons);

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: $"✅ Время: {timeSlotText}\n\n🏠 Выберите комнату:",
            replyMarkup: keyboard,
            cancellationToken: cancellationToken
        );
    }

    private async Task HandleRoomSelectionAsync(long chatId, long userId, string data, CancellationToken cancellationToken)
    {
        if (!data.StartsWith("room_"))
            return;

        var roomIdStr = data.Replace("room_", "");
        if (!int.TryParse(roomIdStr, out var roomId))
            return;

        var stateData = _stateManager.GetData(userId);
        stateData.SelectedRoomId = roomId;

        var room = await _roomService.GetRoomByIdAsync(roomId);

        _stateManager.SetState(userId, UserState.EnteringClientName);

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: $"✅ Комната: {room?.Name}\n\n" +
                  "👤 Введите имя клиента:",
            replyMarkup: KeyboardBuilder.CancelButton(),
            cancellationToken: cancellationToken
        );
    }

    private async Task HandleCategorySelectionAsync(long chatId, long userId, string data, CancellationToken cancellationToken)
    {
        if (!data.StartsWith("category_"))
            return;

        var categoryIdStr = data.Replace("category_", "");
        if (!int.TryParse(categoryIdStr, out var categoryId))
            return;

        var stateData = _stateManager.GetData(userId);
        stateData.SelectedCategoryId = categoryId;

        _stateManager.SetState(userId, UserState.SelectingProduct);

        // Получаем продукты категории
        var products = await _productService.GetProductsByCategoryAsync(categoryId);

        var buttons = products.Select(p =>
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    $"{p.Name} - {p.Price:N0} сум/{GetUnitShortName(p.Unit)}",
                    $"product_{p.Id}"
                )
            }
        ).ToList();

        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData("⬅️ Назад к категориям", "back_to_categories")
        });

        var keyboard = new InlineKeyboardMarkup(buttons);

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "🍽 Выберите блюдо:",
            replyMarkup: keyboard,
            cancellationToken: cancellationToken
        );
    }

    private async Task HandleProductSelectionAsync(long chatId, long userId, string data, CancellationToken cancellationToken)
    {
        if (data == "back_to_categories")
        {
            await ShowCategoriesAsync(chatId, userId, cancellationToken);
            return;
        }

        if (!data.StartsWith("product_"))
            return;

        var productIdStr = data.Replace("product_", "");
        if (!int.TryParse(productIdStr, out var productId))
            return;

        var stateData = _stateManager.GetData(userId);
        stateData.SelectedProductId = productId;

        var product = await _productService.GetProductByIdAsync(productId);

        _stateManager.SetState(userId, UserState.EnteringQuantity);

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: $"✅ Выбрано: {product?.Name}\n" +
                  $"💰 Цена: {product?.Price:N0} сум/{GetUnitShortName(product!.Unit)}\n\n" +
                  $"📝 Введите количество ({GetUnitShortName(product.Unit)}):",
            replyMarkup: KeyboardBuilder.CancelButton(),
            cancellationToken: cancellationToken
        );
    }

    private async Task ShowCategoriesAsync(long chatId, long userId, CancellationToken cancellationToken)
    {
        _stateManager.SetState(userId, UserState.SelectingCategory);

        var categories = await _productService.GetActiveCategoriesAsync();

        var buttons = categories.Select(c =>
            new[]
            {
                InlineKeyboardButton.WithCallbackData($"📂 {c.Name}", $"category_{c.Id}")
            }
        ).ToList();

        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData("✅ Завершить добавление", "finish_adding"),
            InlineKeyboardButton.WithCallbackData("❌ Отменить заказ", "cancel")
        });

        var keyboard = new InlineKeyboardMarkup(buttons);

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "📋 Выберите категорию блюд:",
            replyMarkup: keyboard,
            cancellationToken: cancellationToken
        );
    }

    private async Task CancelOrderCreationAsync(long chatId, long userId, CancellationToken cancellationToken)
    {
        _stateManager.ClearState(userId);

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "❌ Создание заказа отменено.\n\nИспользуйте /start для возврата в главное меню.",
            cancellationToken: cancellationToken
        );
    }

    public async Task HandleTextMessageAsync(Message message, long userId, CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;
        var text = message.Text ?? string.Empty;
        var currentState = _stateManager.GetState(userId);

        switch (currentState)
        {
            case UserState.EnteringClientName:
                await HandleClientNameInputAsync(chatId, userId, text, cancellationToken);
                break;

            case UserState.EnteringClientPhone:
                await HandleClientPhoneInputAsync(chatId, userId, text, cancellationToken);
                break;

            case UserState.EnteringGuestCount:
                await HandleGuestCountInputAsync(chatId, userId, text, cancellationToken);
                break;

            case UserState.EnteringQuantity:
                await HandleQuantityInputAsync(chatId, userId, text, cancellationToken);
                break;

            default:
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "❓ Используйте кнопки меню или команды.",
                    cancellationToken: cancellationToken
                );
                break;
        }
    }

    private async Task HandleClientNameInputAsync(long chatId, long userId, string name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Имя не может быть пустым. Попробуйте еще раз:",
                cancellationToken: cancellationToken
            );
            return;
        }

        var stateData = _stateManager.GetData(userId);
        stateData.ClientName = name;

        _stateManager.SetState(userId, UserState.EnteringClientPhone);

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: $"✅ Имя: {name}\n\n📞 Введите номер телефона клиента:",
            replyMarkup: KeyboardBuilder.CancelButton(),
            cancellationToken: cancellationToken
        );
    }

    private async Task HandleClientPhoneInputAsync(long chatId, long userId, string phone, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Телефон не может быть пустым. Попробуйте еще раз:",
                cancellationToken: cancellationToken
            );
            return;
        }

        var stateData = _stateManager.GetData(userId);
        stateData.ClientPhone = phone;

        _stateManager.SetState(userId, UserState.EnteringGuestCount);

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: $"✅ Телефон: {phone}\n\n👥 Введите количество гостей:",
            replyMarkup: KeyboardBuilder.CancelButton(),
            cancellationToken: cancellationToken
        );
    }

    private async Task HandleGuestCountInputAsync(long chatId, long userId, string countStr, CancellationToken cancellationToken)
    {
        if (!int.TryParse(countStr, out var count) || count < 1)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Введите корректное количество гостей (число больше 0):",
                cancellationToken: cancellationToken
            );
            return;
        }

        var stateData = _stateManager.GetData(userId);
        stateData.GuestCount = count;

        // Переходим к выбору блюд
        await ShowCategoriesAsync(chatId, userId, cancellationToken);
    }

    private async Task HandleQuantityInputAsync(long chatId, long userId, string quantityStr, CancellationToken cancellationToken)
    {
        if (!decimal.TryParse(quantityStr, out var quantity) || quantity <= 0)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Введите корректное количество (число больше 0):",
                cancellationToken: cancellationToken
            );
            return;
        }

        var stateData = _stateManager.GetData(userId);
        var product = await _productService.GetProductByIdAsync(stateData.SelectedProductId!.Value);

        if (product == null)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Ошибка: продукт не найден.",
                cancellationToken: cancellationToken
            );
            return;
        }

        // Добавляем в корзину
        var orderItem = new OrderItemData
        {
            ProductId = product.Id,
            ProductName = product.Name,
            Quantity = quantity,
            Unit = product.Unit,
            Price = product.Price,
            Subtotal = quantity * product.Price
        };

        stateData.Cart.Add(orderItem);

        // Показываем что добавлено и спрашиваем что дальше
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: $"✅ Добавлено:\n" +
                  $"{product.Name} - {quantity} {GetUnitShortName(product.Unit)} × {product.Price:N0} = {orderItem.Subtotal:N0} сум\n\n" +
                  $"🛒 В корзине: {stateData.Cart.Count} позиций\n" +
                  $"💰 Сумма: {stateData.Cart.Sum(i => i.Subtotal):N0} сум",
            cancellationToken: cancellationToken
        );

        // Показываем категории снова
        await ShowCategoriesAsync(chatId, userId, cancellationToken);
    }

    private string GetUnitShortName(ProductUnit unit)
    {
        return unit switch
        {
            ProductUnit.Piece => "шт",
            ProductUnit.Kg => "кг",
            ProductUnit.Gram => "гр",
            ProductUnit.Liter => "л",
            ProductUnit.Ml => "мл",
            _ => ""
        };
    }

    private async Task FinishOrderCreationAsync(long chatId, long userId, CancellationToken cancellationToken)
{
    var stateData = _stateManager.GetData(userId);

    if (stateData.Cart.Count == 0)
    {
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "❌ Корзина пуста! Добавьте хотя бы одно блюдо.",
            cancellationToken: cancellationToken
        );
        await ShowCategoriesAsync(chatId, userId, cancellationToken);
        return;
    }

    // Получаем данные работника
    using var scope = _serviceProvider.CreateScope();
    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
    var employee = await unitOfWork.Employees.GetByTelegramIdAsync(userId);

    if (employee == null)
    {
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "❌ Ошибка: работник не найден.",
            cancellationToken: cancellationToken
        );
        return;
    }

    try
    {
        // Создаём заказ
        var order = await _orderService.CreateOrderAsync(
            roomId: stateData.SelectedRoomId!.Value,
            employeeId: employee.Id,
            clientName: stateData.ClientName!,
            clientPhone: stateData.ClientPhone!,
            guestCount: stateData.GuestCount!.Value,
            bookingDate: stateData.SelectedDate!.Value,
            timeSlot: stateData.SelectedTimeSlot!.Value
        );

        // Добавляем позиции из корзины
        foreach (var item in stateData.Cart)
        {
            await _orderService.AddItemToOrderAsync(
                orderId: order.Id,
                productId: item.ProductId,
                quantity: item.Quantity,
                addedByEmployeeId: employee.Id
            );
        }

        // Формируем красивое сообщение
        var room = await _roomService.GetRoomByIdAsync(stateData.SelectedRoomId.Value);
        var timeSlotText = stateData.SelectedTimeSlot == Core.Enums.TimeSlot.Day 
            ? "День (12:00-16:00)" 
            : "Вечер (17:00-22:00)";

        var message = $"✅ Заказ создан!\n\n" +
                     $"📋 Заказ #{order.OrderNumber}\n" +
                     $"━━━━━━━━━━━━━━━━━━━━\n" +
                     $"👤 {order.ClientName}\n" +
                     $"📞 {order.ClientPhone}\n" +
                     $"👥 Гостей: {order.GuestCount}\n" +
                     $"🏠 {room?.Name}\n" +
                     $"📅 {order.BookingDate:dd.MM.yyyy}\n" +
                     $"⏰ {timeSlotText}\n" +
                     $"━━━━━━━━━━━━━━━━━━━━\n\n" +
                     $"🍽 Заказ:\n";

        foreach (var item in stateData.Cart)
        {
            message += $"• {item.ProductName} - {item.Quantity} {GetUnitShortName(item.Unit)} × {item.Price:N0} = {item.Subtotal:N0} сум\n";
        }

        message += $"\n━━━━━━━━━━━━━━━━━━━━\n" +
                  $"💰 ИТОГО: {stateData.Cart.Sum(i => i.Subtotal):N0} сум";

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: message,
            replyMarkup: KeyboardBuilder.MainMenuKeyboard(employee.Role == Core.Enums.EmployeeRole.Admin),
            cancellationToken: cancellationToken
        );

        // Очищаем состояние
        _stateManager.ClearState(userId);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error creating order");
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: $"❌ Ошибка при создании заказа: {ex.Message}",
            cancellationToken: cancellationToken
        );
    }
}

    
}