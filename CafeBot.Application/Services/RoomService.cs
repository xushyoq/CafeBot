using CafeBot.Core.Entities;
using CafeBot.Core.Enums;
using CafeBot.Core.Interfaces;

namespace CafeBot.Application.Services;

public interface IRoomService
{
    // Чтение
    Task<IEnumerable<Room>> GetActiveRoomsAsync();
    Task<IEnumerable<Room>> GetAvailableRoomsAsync(DateTime date, TimeSlot timeSlot);
    Task<Room?> GetRoomByIdAsync(int roomId);
    Task<bool> IsRoomAvailableAsync(int roomId, DateTime date, TimeSlot timeSlot);

    // CRUD операции с комнатами
    Task<Room> CreateRoomAsync(string name, int? number, int capacity, string? description, string? photoUrl);
    Task<Room?> UpdateRoomAsync(int roomId, string? name, int? number, int? capacity, string? description, string? photoUrl, RoomStatus? status);
    Task<bool> DeleteRoomAsync(int roomId);
    Task<bool> ToggleRoomStatusAsync(int roomId);
}

public class RoomService : IRoomService
{
    private readonly IUnitOfWork _unitOfWork;

    public RoomService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // Методы чтения
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

    // CRUD операции с комнатами
    public async Task<Room> CreateRoomAsync(string name, int? number, int capacity, string? description, string? photoUrl)
    {
        if (capacity <= 0)
        {
            throw new ArgumentException("Вместимость должна быть положительным числом");
        }

        var room = new Room
        {
            Name = name,
            Number = number,
            Capacity = capacity,
            Description = description,
            PhotoUrl = photoUrl,
            Status = RoomStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Rooms.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();

        return room;
    }

    public async Task<Room?> UpdateRoomAsync(int roomId, string? name, int? number, int? capacity, string? description, string? photoUrl, RoomStatus? status)
    {
        var room = await _unitOfWork.Rooms.GetByIdAsync(roomId);
        if (room == null)
        {
            return null;
        }

        if (capacity.HasValue && capacity.Value <= 0)
        {
            throw new ArgumentException("Вместимость должна быть положительным числом");
        }

        if (!string.IsNullOrEmpty(name))
            room.Name = name;

        if (number.HasValue)
            room.Number = number;

        if (capacity.HasValue)
            room.Capacity = capacity.Value;

        if (description != null)
            room.Description = description;

        if (photoUrl != null)
            room.PhotoUrl = photoUrl;

        if (status.HasValue)
            room.Status = status.Value;

        room.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Rooms.UpdateAsync(room);
        await _unitOfWork.SaveChangesAsync();

        return room;
    }

    public async Task<bool> DeleteRoomAsync(int roomId)
    {
        var room = await _unitOfWork.Rooms.GetByIdAsync(roomId);
        if (room == null)
        {
            return false;
        }

        // Проверяем, есть ли активные заказы в этой комнате
        var hasActiveOrders = await _unitOfWork.Orders.HasActiveOrdersInRoomAsync(roomId);
        if (hasActiveOrders)
        {
            throw new InvalidOperationException("Нельзя удалить комнату с активными заказами. Сначала завершите или отмените все заказы в этой комнате.");
        }

        await _unitOfWork.Rooms.DeleteAsync(roomId);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleRoomStatusAsync(int roomId)
    {
        var room = await _unitOfWork.Rooms.GetByIdAsync(roomId);
        if (room == null)
        {
            return false;
        }

        room.Status = room.Status == RoomStatus.Active ? RoomStatus.Inactive : RoomStatus.Active;
        room.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Rooms.UpdateAsync(room);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }
}