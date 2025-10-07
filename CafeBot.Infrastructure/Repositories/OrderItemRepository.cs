using CafeBot.Core.Entities;
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
}