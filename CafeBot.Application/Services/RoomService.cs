using CafeBot.Core.Entities;
using CafeBot.Core.Enums;
using CafeBot.Core.Interfaces;

namespace CafeBot.Application.Services;

public interface IRoomService
{
    Task<IEnumerable<Room>> GetActiveRoomsAsync();
    Task<IEnumerable<Room>> GetAvailableRoomsAsync(DateTime date, TimeSlot timeSlot);
    Task<Room?> GetRoomByIdAsync(int roomId);
    Task<bool> IsRoomAvailableAsync(int roomId, DateTime date, TimeSlot timeSlot);
}

public class RoomService : IRoomService
{
    private readonly IUnitOfWork _unitOfWork;

    public RoomService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<Room>> GetActiveRoomsAsync()
    {
        return await _unitOfWork.Rooms.GetActiveRoomsAsync();
    }

    public async Task<IEnumerable<Room>> GetAvailableRoomsAsync(DateTime date, TimeSlot timeSlot)
    {
        return await _unitOfWork.Rooms.GetAvailableRoomsAsync(date, timeSlot);
    }

    public async Task<Room?> GetRoomByIdAsync(int roomId)
    {
        return await _unitOfWork.Rooms.GetByIdAsync(roomId);
    }

    public async Task<bool> IsRoomAvailableAsync(int roomId, DateTime date, TimeSlot timeSlot)
    {
        return await _unitOfWork.Orders.IsRoomAvailableAsync(roomId, date, timeSlot);
    }
}