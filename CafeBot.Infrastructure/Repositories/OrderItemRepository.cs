using CafeBot.Core.Entities;
using CafeBot.Core.Enums;
using CafeBot.Core.Interfaces;
using CafeBot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CafeBot.Infrastructure.Repositories;

public class OrderItemRepository : BaseRepository<OrderItem>, IOrderItemRepository
{
    public OrderItemRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<OrderItem>> GetOrderItemsByOrderIdAsync(int orderId)
    {
        return await _dbSet
            .Include(oi => oi.Product)
            .Where(oi => oi.OrderId == orderId)
            .OrderBy(oi => oi.AddedAt)
            .ToListAsync();
    }

    public async Task AddRangeAsync(IEnumerable<OrderItem> orderItems)
    {
        await _dbSet.AddRangeAsync(orderItems);
    }

    public async Task<bool> HasProductInOrdersAsync(int productId)
    {
        return await _dbSet.AnyAsync(oi => oi.ProductId == productId);
    }

    public async Task<bool> HasProductInActiveOrdersAsync(int productId)
    {
        return await _dbSet
            .Include(oi => oi.Order)
            .AnyAsync(oi => oi.ProductId == productId &&
                           oi.Order.Status != OrderStatus.Completed &&
                           oi.Order.Status != OrderStatus.Cancelled);
    }
}