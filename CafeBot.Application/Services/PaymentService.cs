using CafeBot.Core.Entities;
using CafeBot.Core.Enums;
using CafeBot.Core.Interfaces;

namespace CafeBot.Application.Services;

public interface IPaymentService
{
    Task<Payment> ProcessPaymentAsync(int orderId, PaymentMethod method, int receivedByEmployeeId, string? notes = null);
    Task<Payment?> GetPaymentByOrderIdAsync(int orderId);
}

public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _unitOfWork;

    public PaymentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Payment> ProcessPaymentAsync(int orderId, PaymentMethod method, 
        int receivedByEmployeeId, string? notes = null)
    {
        var order = await _unitOfWork.Orders.GetOrderWithDetailsAsync(orderId);
        if (order == null)
        {
            throw new ArgumentException("Заказ не найден");
        }

        if (order.Status == OrderStatus.Paid || order.Status == OrderStatus.Completed)
        {
            throw new InvalidOperationException("Заказ уже оплачен");
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            throw new InvalidOperationException("Нельзя оплатить отмененный заказ");
        }

        // Создаём платеж
        var payment = new Payment
        {
            OrderId = orderId,
            Amount = order.TotalAmount,
            Method = method,
            ReceivedByEmployeeId = receivedByEmployeeId,
            Status = PaymentStatus.Completed,
            PaidAt = DateTime.UtcNow,
            Notes = notes
        };

        await _unitOfWork.Payments.AddAsync(payment);

        // Обновляем статус заказа
        order.Status = OrderStatus.Paid;
        order.PaidAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Orders.UpdateAsync(order);
        await _unitOfWork.SaveChangesAsync();

        return payment;
    }

    public async Task<Payment?> GetPaymentByOrderIdAsync(int orderId)
    {
        return await _unitOfWork.Payments.GetPaymentByOrderIdAsync(orderId);
    }
}