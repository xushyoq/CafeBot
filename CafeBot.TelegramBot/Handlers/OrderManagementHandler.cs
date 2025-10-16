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
                text: "❌ Заказ не найден.",
                cancellationToken: cancellationToken
            );
            return;
        }

        if (order.Status != OrderStatus.Created)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Только новые заказы можно подтверждать.",
                cancellationToken: cancellationToken
            );
            return;
        }

        await _orderService.UpdateOrderStatusAsync(orderId, OrderStatus.Confirmed);

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: $"✅ Заказ #{order.OrderNumber} подтверждён!\n\n" +
                  $"Клиент: {order.ClientName}\n" +
                  $"Телефон: {order.ClientPhone}\n" +
                  $"Комната: {order.Room.Name}\n" +
                  $"Дата: {order.BookingDate:dd.MM.yyyy}\n" +
                  $"Время: {GetTimeSlotText(order.TimeSlot)}\n\n" +
                  $"Ожидаем клиента в указанное время.",
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
                text: "❌ Заказ не найден.",
                cancellationToken: cancellationToken
            );
            return;
        }

        if (order.Status != OrderStatus.Confirmed && order.Status != OrderStatus.Created)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Только подтверждённые заказы можно активировать.",
                cancellationToken: cancellationToken
            );
            return;
        }

        await _orderService.UpdateOrderStatusAsync(orderId, OrderStatus.Active);

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: $"🔥 Заказ #{order.OrderNumber} активирован!\n\n" +
                  $"Клиент: {order.ClientName}\n" +
                  $"Комната: {order.Room.Name}\n" +
                  $"Сумма: {order.TotalAmount:N0} сум\n\n" +
                  $"Клиент в комнате, приятного аппетита! 🍽",
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
                text: "❌ Заказ не найден.",
                cancellationToken: cancellationToken
            );
            return;
        }

        if (!order.CanBeCancelled())
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Этот заказ нельзя отменить (уже активен или завершён).",
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
                    text: $"❌ Заказ #{order.OrderNumber} отменён\n\n" +
                          $"Клиент: {order.ClientName}\n" +
                          $"Телефон: {order.ClientPhone}\n" +
                          $"Комната: {order.Room.Name}\n" +
                          $"Дата: {order.BookingDate:dd.MM.yyyy}\n\n" +
                          $"Комната освобождена.",
                    cancellationToken: cancellationToken
                );

                _logger.LogInformation("Order {OrderId} cancelled by user {UserId}", orderId, userId);
            }
            else
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "❌ Ошибка при отмене заказа.",
                    cancellationToken: cancellationToken
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling order {OrderId}", orderId);
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"❌ Ошибка: {ex.Message}",
                cancellationToken: cancellationToken
            );
        }
    }

    private string GetTimeSlotText(TimeSlot timeSlot)
    {
        return timeSlot == TimeSlot.Day ? "День (12:00-16:00)" : "Вечер (17:00-22:00)";
    }
}