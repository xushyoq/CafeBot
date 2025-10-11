using CafeBot.Application.Services;
using CafeBot.Core.Enums;
using CafeBot.Core.Interfaces;
using CafeBot.TelegramBot.Keyboards;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace CafeBot.TelegramBot.Handlers;

public class PaymentHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly IOrderService _orderService;
    private readonly IPaymentService _paymentService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PaymentHandler> _logger;

    public PaymentHandler(
        ITelegramBotClient botClient,
        IOrderService orderService,
        IPaymentService paymentService,
        IUnitOfWork unitOfWork,
        ILogger<PaymentHandler> logger)
    {
        _botClient = botClient;
        _orderService = orderService;
        _paymentService = paymentService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task PrepareOrderForPaymentAsync(long chatId, int orderId, CancellationToken cancellationToken)
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

        if (order.Status == OrderStatus.ReadyToPay || order.Status == OrderStatus.Paid)
        {
            await ShowPaymentOptionsAsync(chatId, order, cancellationToken);
            return;
        }

        // Переводим заказ в статус ReadyToPay
        await _orderService.UpdateOrderStatusAsync(orderId, OrderStatus.ReadyToPay);
        
        // Перезагружаем заказ
        order = await _orderService.GetOrderWithDetailsAsync(orderId);
        
        await ShowPaymentOptionsAsync(chatId, order!, cancellationToken);
    }

    private async Task ShowPaymentOptionsAsync(long chatId, Core.Entities.Order order, CancellationToken cancellationToken)
    {
        var timeSlotText = order.TimeSlot == TimeSlot.Day 
            ? "День (12:00-16:00)" 
            : "Вечер (17:00-22:00)";

        var message = $"💰 Заказ готов к оплате\n\n" +
                     $"📋 Заказ #{order.OrderNumber}\n" +
                     $"━━━━━━━━━━━━━━━━━━━━\n" +
                     $"👤 {order.ClientName}\n" +
                     $"📞 {order.ClientPhone}\n" +
                     $"👥 Гостей: {order.GuestCount}\n" +
                     $"🏠 {order.Room.Name}\n" +
                     $"📅 {order.BookingDate:dd.MM.yyyy}\n" +
                     $"⏰ {timeSlotText}\n" +
                     $"━━━━━━━━━━━━━━━━━━━━\n\n" +
                     $"🍽 Заказ:\n";

        foreach (var item in order.OrderItems)
        {
            var quantityText = FormatQuantity(item.Quantity, item.Unit);
            message += $"• {item.ProductName}\n" +
                      $"  {quantityText} × {item.Price:N0} = {item.Subtotal:N0} сум\n";
        }

        message += $"\n━━━━━━━━━━━━━━━━━━━━\n" +
                  $"💰 К ОПЛАТЕ: {order.TotalAmount:N0} сум\n" +
                  $"━━━━━━━━━━━━━━━━━━━━\n\n" +
                  $"Выберите способ оплаты:";

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("💵 Наличные", $"pay_cash_{order.Id}"),
                InlineKeyboardButton.WithCallbackData("💳 Карта", $"pay_card_{order.Id}")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📱 Перевод", $"pay_transfer_{order.Id}")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("⬅️ Назад к заказу", $"vieworder_{order.Id}")
            }
        });

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: message,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken
        );
    }

    public async Task ProcessPaymentAsync(long chatId, long userId, int orderId, PaymentMethod method, CancellationToken cancellationToken)
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

        try
        {
            // Создаем платеж
            var payment = await _paymentService.ProcessPaymentAsync(
                orderId: orderId,
                method: method,
                receivedByEmployeeId: employee.Id
            );

            // Получаем обновленный заказ
            var order = await _orderService.GetOrderWithDetailsAsync(orderId);

            if (order == null)
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "❌ Ошибка при получении заказа.",
                    cancellationToken: cancellationToken
                );
                return;
            }

            // Завершаем заказ
            await _orderService.UpdateOrderStatusAsync(orderId, OrderStatus.Completed);

            var methodText = method switch
            {
                PaymentMethod.Cash => "💵 Наличными",
                PaymentMethod.Card => "💳 Картой",
                PaymentMethod.Transfer => "📱 Переводом",
                _ => "Неизвестно"
            };

            var timeSlotText = order.TimeSlot == TimeSlot.Day 
                ? "День (12:00-16:00)" 
                : "Вечер (17:00-22:00)";

            var message = $"✅ Оплата принята!\n\n" +
                         $"📋 Заказ #{order.OrderNumber}\n" +
                         $"━━━━━━━━━━━━━━━━━━━━\n" +
                         $"👤 {order.ClientName}\n" +
                         $"📞 {order.ClientPhone}\n" +
                         $"👥 Гостей: {order.GuestCount}\n" +
                         $"🏠 {order.Room.Name}\n" +
                         $"📅 {order.BookingDate:dd.MM.yyyy}\n" +
                         $"⏰ {timeSlotText}\n" +
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
                      $"💳 Способ оплаты: {methodText}\n" +
                      $"✅ Оплачено: {payment.Amount:N0} сум\n" +
                      $"🕐 Время: {payment.PaidAt:dd.MM.yyyy HH:mm}\n\n" +
                      $"Спасибо! Заказ завершен! 🎉";

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: message,
                replyMarkup: KeyboardBuilder.MainMenuKeyboard(employee.Role == EmployeeRole.Admin),
                cancellationToken: cancellationToken
            );

            _logger.LogInformation("Payment processed for order {OrderId} by employee {EmployeeId}", orderId, employee.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment for order {OrderId}", orderId);
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"❌ Ошибка при обработке оплаты: {ex.Message}",
                cancellationToken: cancellationToken
            );
        }
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