using CafeBot.Core.Entities;
using CafeBot.Core.Enums;
using CafeBot.Core.Interfaces;
using CafeBot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CafeBot.Infrastructure.Repositories;

public class RoomRepository : BaseRepository<Room>, IRoomRepository
{
    public RoomRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Room>> GetActiveRoomsAsync()
    {
        return await _dbSet
            .Where(r => r.Status == RoomStatus.Active)
            .OrderBy(r => r.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Room>> GetAvailableRoomsAsync(DateTime date, TimeSlot timeSlot)
    {
        var occupiedRoomIds = await _context.Orders
            .Where(o => o.BookingDate.Date == date.Date
                && o.TimeSlot == timeSlot
                && o.Status != OrderStatus.Cancelled
                && o.Status != OrderStatus.Completed)
            .Select(o => o.RoomId)
            .ToListAsync();

        return await _dbSet
            .Where(r => r.Status == RoomStatus.Active
                && !occupiedRoomIds.Contains(r.Id))
            .OrderBy(r => r.Name)
            .ToListAsync();
    }

    public async Task<Room?> GetRoomWithOrdersAsync(int roomId)
    {
        return await _dbSet
            .Include(r => r.Orders)
            .FirstOrDefaultAsync(r => r.Id == roomId);
    }
}