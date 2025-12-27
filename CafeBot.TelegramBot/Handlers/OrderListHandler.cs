using CafeBot.Application.Services;
using CafeBot.Core.Enums;
using CafeBot.Core.Interfaces;
using CafeBot.TelegramBot.Keyboards;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Linq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace CafeBot.TelegramBot.Handlers;

public class OrderListHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly IOrderService _orderService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderListHandler> _logger;

    public OrderListHandler(
        ITelegramBotClient botClient,
        IOrderService orderService,
        IUnitOfWork unitOfWork,
        ILogger<OrderListHandler> logger)
    {
        _botClient = botClient;
        _orderService = orderService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task ShowMyOrdersAsync(long chatId, long userId, CancellationToken cancellationToken)
    {
        var employee = await _unitOfWork.Employees.GetByTelegramIdAsync(userId);
        if (employee == null)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Работник не найден.",
                cancellationToken: cancellationToken
            );
            return;
        }

        // Получаем активные заказы этого работника
        var orders = await _unitOfWork.Orders.GetOrdersByEmployeeAsync(employee.Id);

        var activeOrders = orders.Where(o =>
            o.Status == OrderStatus.Created ||
            o.Status == OrderStatus.Confirmed ||
            o.Status == OrderStatus.Active ||
            o.Status == OrderStatus.ReadyToPay
        ).OrderByDescending(o => o.CreatedAt).ToList();

        if (!activeOrders.Any())
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "📝 У вас нет активных заказов.",
                replyMarkup: KeyboardBuilder.MainMenuKeyboard(employee.Role == EmployeeRole.Admin),
                cancellationToken: cancellationToken
            );
            return;
        }

        // Формируем список кнопок с заказами
        var buttons = activeOrders.Select(order =>
        {
            var statusEmoji = GetStatusEmoji(order.Status);
            var timeSlotText = order.TimeSlot == TimeSlot.Day ? "Kun" : "Kechqurun";

            return new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    $"{statusEmoji} {order.OrderNumber} | {order.ClientName} | {order.BookingDate:dd.MM} {timeSlotText}",
                    $"vieworder_{order.Id}"
                )
            };
        }).ToList();

        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData("🔄 Yangilash", "refresh_orders")
        });

        var keyboard = new InlineKeyboardMarkup(buttons);

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: $"📋 Sizning faol buyurtmalaringiz ({activeOrders.Count}):\n\n" +
                  "Tafsilotlarni ko'rish uchun buyurtmani bosing:",
            replyMarkup: keyboard,
            cancellationToken: cancellationToken
        );
    }

    public async Task ShowOrderDetailsAsync(long chatId, int orderId, CancellationToken cancellationToken)
    {
        var order = await _orderService.GetOrderWithDetailsAsync(orderId);

        if (order == null)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Заказ не найден.",
                cancellationToken: cancellationToken
            );
            return;
        }

        var statusEmoji = GetStatusEmoji(order.Status);
        var statusText = GetStatusText(order.Status);
        var timeSlotText = order.TimeSlot == TimeSlot.Day
            ? "Kun (12:00-16:00)"
            : "Kechqurun (17:00-22:00)";

        var message = $"{statusEmoji} Заказ #{order.OrderNumber}\n" +
                     $"━━━━━━━━━━━━━━━━━━━━\n" +
                     $"📊 Holat: {statusText}\n" +
                     $"👤 Mijoz: {order.ClientName}\n" +
                     $"📞 Telefon: {order.ClientPhone}\n" +
                     $"👥 Mehmonlar: {order.GuestCount}\n" +
                     $"🏠 Xona: {order.Room.Name}\n" +
                     $"📅 Sana: {order.BookingDate:dd.MM.yyyy}\n" +
                     $"⏰ Vaqt: {timeSlotText}\n" +
                     $"━━━━━━━━━━━━━━━━━━━━\n\n" +
                     $"🍽 Buyurtma:\n";

        foreach (var item in order.OrderItems)
        {
            var quantityText = FormatQuantity(item.Quantity, item.Unit);
            message += $"• {item.ProductName}\n" +
                      $"  {quantityText} × {item.Price:N0} = {item.Subtotal:N0} сум\n";
        }

        message += $"\n━━━━━━━━━━━━━━━━━━━━\n" +
                  $"💰 JAMI: {order.TotalAmount:N0} so'm\n" +
                  $"━━━━━━━━━━━━━━━━━━━━\n\n" +
                  $"🕐 Yaratilgan: {order.CreatedAt:dd.MM.yyyy HH:mm}";

        // Кнопки действий в зависимости от статуса
        var buttons = new List<InlineKeyboardButton[]>();

        if (order.Status == OrderStatus.Created)
        {
            buttons.Add(new[]
            {
        InlineKeyboardButton.WithCallbackData("✅ Tasdiqlash", $"confirm_order_{orderId}"),
        InlineKeyboardButton.WithCallbackData("🔥 Faollashtirish", $"activate_order_{orderId}")
    });
            buttons.Add(new[]
            {
        InlineKeyboardButton.WithCallbackData("➕ Qo'shimcha buyurtma", $"addmore_{orderId}"),
        InlineKeyboardButton.WithCallbackData("💰 To'lovga", $"topayment_{orderId}")
    });
            buttons.Add(new[]
            {
        InlineKeyboardButton.WithCallbackData("❌ Buyurtmani bekor qilish", $"cancel_order_{orderId}")
    });
        }
        else if (order.Status == OrderStatus.Confirmed)
        {
            buttons.Add(new[]
            {
        InlineKeyboardButton.WithCallbackData("🔥 Mijoz keldi", $"activate_order_{orderId}"),
        InlineKeyboardButton.WithCallbackData("➕ Qo'shimcha buyurtma", $"addmore_{orderId}")
    });
            buttons.Add(new[]
            {
        InlineKeyboardButton.WithCallbackData("💰 To'lovga", $"topayment_{orderId}"),
        InlineKeyboardButton.WithCallbackData("❌ Отменить", $"cancel_order_{orderId}")
    });
        }
        else if (order.Status == OrderStatus.Active)
        {
            buttons.Add(new[]
            {
        InlineKeyboardButton.WithCallbackData("➕ Qo'shimcha buyurtma", $"addmore_{orderId}"),
        InlineKeyboardButton.WithCallbackData("💰 To'lovga", $"topayment_{orderId}")
    });
        }
        else if (order.Status == OrderStatus.ReadyToPay)
        {
            buttons.Add(new[]
            {
        InlineKeyboardButton.WithCallbackData("💵 Naqd to'lov", $"pay_cash_{orderId}"),
        InlineKeyboardButton.WithCallbackData("💳 Karta orqali to'lov", $"pay_card_{orderId}")
    });
            buttons.Add(new[]
            {
        InlineKeyboardButton.WithCallbackData("📱 O'tkazma", $"pay_transfer_{orderId}")
    });
        }

        buttons.Add(new[]
        {
    InlineKeyboardButton.WithCallbackData("⬅️ Buyurtmalar ro'yxatiga", "back_to_orders")
});

        var keyboard = new InlineKeyboardMarkup(buttons);

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: message,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken
        );
    }

    private string GetStatusEmoji(OrderStatus status)
    {
        return status switch
        {
            OrderStatus.Created => "🆕",
            OrderStatus.Confirmed => "✅",
            OrderStatus.Active => "🔥",
            OrderStatus.ReadyToPay => "💰",
            OrderStatus.Paid => "✔️",
            OrderStatus.Completed => "🏁",
            OrderStatus.Cancelled => "❌",
            _ => "❓"
        };
    }

    private string GetStatusText(OrderStatus status)
    {
        return status switch
        {
            OrderStatus.Created => "Yaratildi",
            OrderStatus.Confirmed => "Tasdiqlandi",
            OrderStatus.Active => "Faol",
            OrderStatus.ReadyToPay => "To'lovga tayyor",
            OrderStatus.Paid => "To'landi",
            OrderStatus.Completed => "Tugagan",
            OrderStatus.Cancelled => "Bekor qilingan",
            _ => "Noma'lum"
        };
    }

    private string FormatQuantity(decimal quantity, ProductUnit unit)
    {
        return unit switch
        {
            ProductUnit.Piece => $"{quantity:0.##} шт",
            ProductUnit.Kg => $"{quantity:0.##} кг",
            ProductUnit.Gram => $"{quantity:0} гр",
            ProductUnit.Liter => $"{quantity:0.##} л",
            ProductUnit.Ml => $"{quantity:0} мл",
            _ => quantity.ToString()
        };
    }
}