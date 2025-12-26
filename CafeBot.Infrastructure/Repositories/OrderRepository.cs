using CafeBot.Core.Entities;
using CafeBot.Core.Enums;
using CafeBot.Core.Interfaces;
using CafeBot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CafeBot.Infrastructure.Repositories;

public class OrderRepository : BaseRepository<Order>, IOrderRepository
{
    public OrderRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Order?> GetOrderWithDetailsAsync(int orderId)
    {
        return await _dbSet
            .Include(o => o.Room)
            .Include(o => o.Employee)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }

    public async Task<IEnumerable<Order>> GetOrdersByStatusAsync(OrderStatus status)
    {
        return await _dbSet
            .Include(o => o.Room)
            .Include(o => o.Employee)
            .Where(o => o.Status == status)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Order>> GetOrdersByDateAsync(DateTime date)
    {
        return await _dbSet
            .Include(o => o.Room)
            .Include(o => o.Employee)
            .Where(o => o.BookingDate.Date == date.Date)
            .OrderBy(o => o.TimeSlot)
            .ThenBy(o => o.Room.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Order>> GetOrdersByEmployeeAsync(int employeeId)
    {
        return await _dbSet
            .Include(o => o.Room)
            .Where(o => o.EmployeeId == employeeId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Order>> GetOrdersByRoomAsync(int roomId, DateTime date)
    {
        return await _dbSet
            .Include(o => o.Employee)
            .Where(o => o.RoomId == roomId && o.BookingDate.Date == date.Date)
            .OrderBy(o => o.TimeSlot)
            .ToListAsync();
    }

    public async Task<IEnumerable<Order>> GetOrdersByEmployeeAndDateRangeAsync(int employeeId, DateTime startDate, DateTime endDate, OrderStatus[] statuses)
    {
        return await _dbSet
            .Include(o => o.Room)
            .Where(o => o.EmployeeId == employeeId
                     && o.CreatedAt >= startDate
                     && o.CreatedAt < endDate
                     && statuses.Contains(o.Status))
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Order>> GetOrdersByEmployeeAndStatusAsync(int employeeId, OrderStatus[] statuses)
    {
        return await _dbSet
            .Include(o => o.Room)
            .Where(o => o.EmployeeId == employeeId && statuses.Contains(o.Status))
            .OrderBy(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<string> GenerateOrderNumberAsync()
    {
        var today = DateTime.UtcNow.Date;
        var todayOrderCount = await _dbSet
            .Where(o => o.CreatedAt.Date == today)
            .CountAsync();

        var sequenceNumber = todayOrderCount + 1;
        return $"ORD-{today:yyyyMMdd}-{sequenceNumber:000}";
    }

    public async Task<bool> IsRoomAvailableAsync(int roomId, DateTime date, TimeSlot timeSlot)
    {
        return !await _dbSet.AnyAsync(o =>
            o.RoomId == roomId
            && o.BookingDate.Date == date.Date
            && o.TimeSlot == timeSlot
            && o.Status != OrderStatus.Cancelled
            && o.Status != OrderStatus.Completed);
    }

    public async Task<bool> HasActiveOrdersInRoomAsync(int roomId)
    {
        return await _dbSet.AnyAsync(o =>
            o.RoomId == roomId
            && o.Status != OrderStatus.Cancelled
            && o.Status != OrderStatus.Completed);
    }
}