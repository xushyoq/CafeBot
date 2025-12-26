using CafeBot.Core.Entities;

namespace CafeBot.Core.Interfaces;

public interface IOrderItemRepository : IRepository<OrderItem>
{
    Task<IEnumerable<OrderItem>> GetOrderItemsByOrderIdAsync(int orderId);
    Task AddRangeAsync(IEnumerable<OrderItem> orderItems);
    Task<bool> HasProductInOrdersAsync(int productId);
    Task<bool> HasProductInActiveOrdersAsync(int productId);
}