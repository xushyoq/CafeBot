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
            var timeSlotText = order.TimeSlot == TimeSlot.Day ? "День" : "Вечер";

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
            InlineKeyboardButton.WithCallbackData("🔄 Обновить", "refresh_orders")
        });

        var keyboard = new InlineKeyboardMarkup(buttons);

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: $"📋 Ваши активные заказы ({activeOrders.Count}):\n\n" +
                  "Нажмите на заказ для просмотра деталей:",
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
            ? "День (12:00-16:00)"
            : "Вечер (17:00-22:00)";

        var message = $"{statusEmoji} Заказ #{order.OrderNumber}\n" +
                     $"━━━━━━━━━━━━━━━━━━━━\n" +
                     $"📊 Статус: {statusText}\n" +
                     $"👤 Клиент: {order.ClientName}\n" +
                     $"📞 Телефон: {order.ClientPhone}\n" +
                     $"👥 Гостей: {order.GuestCount}\n" +
                     $"🏠 Комната: {order.Room.Name}\n" +
                     $"📅 Дата: {order.BookingDate:dd.MM.yyyy}\n" +
                     $"⏰ Время: {timeSlotText}\n" +
                     $"━━━━━━━━━━━━━━━━━━━━\n\n" +
                     $"🍽 Заказ:\n";

        foreach (var item in order.OrderItems)
        {
            var quantityText = FormatQuantity(item.Quantity, item.Unit);
            message += $"• {item.ProductName}\n" +
                      $"  {quantityText} × {item.Price:N0} = {item.Subtotal:N0} сум\n";
        }

        message += $"\n━━━━━━━━━━━━━━━━━━━━\n" +
                  $"💰 ИТОГО: {order.TotalAmount:N0} сум\n" +
                  $"━━━━━━━━━━━━━━━━━━━━\n\n" +
                  $"🕐 Создан: {order.CreatedAt:dd.MM.yyyy HH:mm}";

        // Кнопки действий в зависимости от статуса
        var buttons = new List<InlineKeyboardButton[]>();

        if (order.Status == OrderStatus.Created || order.Status == OrderStatus.Confirmed)
        {
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData("✅ Подтвердить", $"confirm_order_{orderId}"),
                InlineKeyboardButton.WithCallbackData("❌ Отменить", $"cancel_order_{orderId}")
            });
        }

        if (order.Status == OrderStatus.Created || order.Status == OrderStatus.Confirmed || order.Status == OrderStatus.Active)
        {
            buttons.Add(new[]
            {
        InlineKeyboardButton.WithCallbackData("➕ Дозаказать", $"addmore_{orderId}"),
        InlineKeyboardButton.WithCallbackData("💰 К оплате", $"topayment_{orderId}")
    });
        }

        if (order.Status == OrderStatus.ReadyToPay)
        {
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData("💵 Оплата наличными", $"pay_cash_{orderId}"),
                InlineKeyboardButton.WithCallbackData("💳 Оплата картой", $"pay_card_{orderId}")
            });
        }

        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData("⬅️ К списку заказов", "back_to_orders")
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
            OrderStatus.Created => "Создан",
            OrderStatus.Confirmed => "Подтвержден",
            OrderStatus.Active => "Активен",
            OrderStatus.ReadyToPay => "Готов к оплате",
            OrderStatus.Paid => "Оплачен",
            OrderStatus.Completed => "Завершен",
            OrderStatus.Cancelled => "Отменен",
            _ => "Неизвестно"
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