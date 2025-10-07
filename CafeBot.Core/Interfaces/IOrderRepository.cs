using CafeBot.Core.Entities;
using CafeBot.Core.Enums;

namespace CafeBot.Core.Interfaces;

public interface IOrderRepository : IRepository<Order>
{
    Task<Order?> GetOrderWithDetailsAsync(int orderId);
    Task<IEnumerable<Order>> GetOrdersByStatusAsync(OrderStatus status);
    Task<IEnumerable<Order>> GetOrdersByDateAsync(DateTime date);
    Task<IEnumerable<Order>> GetOrdersByEmployeeAsync(int employeeId);
    Task<IEnumerable<Order>> GetOrdersByRoomAsync(int roomId, DateTime date);
    Task<string> GenerateOrderNumberAsync();
    Task<bool> IsRoomAvailableAsync(int roomId, DateTime date, TimeSlot timeSlot);
}