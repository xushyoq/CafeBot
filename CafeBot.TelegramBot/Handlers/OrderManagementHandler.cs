using CafeBot.Application.Services;
using CafeBot.Core.Enums;
using CafeBot.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CafeBot.TelegramBot.Handlers;

public class OrderManagementHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly IOrderService _orderService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderManagementHandler> _logger;

    public OrderManagementHandler(
        ITelegramBotClient botClient,
        IOrderService orderService,
        IUnitOfWork unitOfWork,
        ILogger<OrderManagementHandler> logger)
    {
        _botClient = botClient;
        _orderService = orderService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task ConfirmOrderAsync(long chatId, int orderId, CancellationToken cancellationToken)
    {
        var order = await _orderService.GetOrderWithDetailsAsync(orderId);
        
        if (order == null)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Buyurtma topilmadi.",
                cancellationToken: cancellationToken
            );
            return;
        }

        if (order.Status != OrderStatus.Created)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Faqat yangi buyurtmalarni tasdiqlash mumkin.",
                cancellationToken: cancellationToken
            );
            return;
        }

        await _orderService.UpdateOrderStatusAsync(orderId, OrderStatus.Confirmed);

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: $"✅ Buyurtma #{order.OrderNumber} tasdiqlandi!\n\n" +
                  $"Mijoz: {order.ClientName}\n" +
                  $"Telefon: {order.ClientPhone}\n" +
                  $"Xona: {order.Room.Name}\n" +
                  $"Sana: {order.BookingDate:dd.MM.yyyy}\n" +
                  $"Vaqt: {GetTimeSlotText(order.TimeSlot)}\n\n" +
                  $"Mijozni ko'rsatilgan vaqtda kutamiz.",
            cancellationToken: cancellationToken
        );

        _logger.LogInformation("Order {OrderId} confirmed", orderId);
    }

    public async Task ActivateOrderAsync(long chatId, int orderId, CancellationToken cancellationToken)
    {
        var order = await _orderService.GetOrderWithDetailsAsync(orderId);
        
        if (order == null)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Buyurtma topilmadi.",
                cancellationToken: cancellationToken
            );
            return;
        }

        if (order.Status != OrderStatus.Confirmed && order.Status != OrderStatus.Created)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Faqat tasdiqlangan buyurtmalarni faollashtirish mumkin.",
                cancellationToken: cancellationToken
            );
            return;
        }

        await _orderService.UpdateOrderStatusAsync(orderId, OrderStatus.Active);

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: $"🔥 Buyurtma #{order.OrderNumber} faollashtirildi!\n\n" +
                  $"Mijoz: {order.ClientName}\n" +
                  $"Xona: {order.Room.Name}\n" +
                  $"Summa: {order.TotalAmount:N0} so'm\n\n" +
                  $"Mijoz xonada, ishtaha och gullay! 🍽",
            cancellationToken: cancellationToken
        );

        _logger.LogInformation("Order {OrderId} activated", orderId);
    }

    public async Task CancelOrderAsync(long chatId, long userId, int orderId, CancellationToken cancellationToken)
    {
        var order = await _orderService.GetOrderWithDetailsAsync(orderId);
        
        if (order == null)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Buyurtma topilmadi.",
                cancellationToken: cancellationToken
            );
            return;
        }

        if (!order.CanBeCancelled())
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Bu buyurtmani bekor qilib bo'lmaydi (allaqachon faol yoki tugagan).",
                cancellationToken: cancellationToken
            );
            return;
        }

        try
        {
            var success = await _orderService.CancelOrderAsync(orderId);
            
            if (success)
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"❌ Buyurtma #{order.OrderNumber} bekor qilindi\n\n" +
                          $"Mijoz: {order.ClientName}\n" +
                          $"Telefon: {order.ClientPhone}\n" +
                          $"Xona: {order.Room.Name}\n" +
                          $"Sana: {order.BookingDate:dd.MM.yyyy}\n\n" +
                          $"Xona bo'shatildi.",
                    cancellationToken: cancellationToken
                );

                _logger.LogInformation("Order {OrderId} cancelled by user {UserId}", orderId, userId);
            }
            else
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "❌ Buyurtmani bekor qilishda xatolik.",
                    cancellationToken: cancellationToken
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling order {OrderId}", orderId);
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"❌ Xatolik: {ex.Message}",
                cancellationToken: cancellationToken
            );
        }
    }

    private string GetTimeSlotText(TimeSlot timeSlot)
    {
        return timeSlot == TimeSlot.Day ? "Kun (12:00-16:00)" : "Kechqurun (17:00-22:00)";
    }
}