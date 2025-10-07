using CafeBot.Core.Entities;
using CafeBot.Core.Enums;

namespace CafeBot.Core.Interfaces;

public interface IRoomRepository : IRepository<Room>
{
    Task<IEnumerable<Room>> GetActiveRoomsAsync();
    Task<IEnumerable<Room>> GetAvailableRoomsAsync(DateTime date, TimeSlot timeSlot);
    Task<Room?> GetRoomWithOrdersAsync(int roomId);
}