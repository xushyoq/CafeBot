using CafeBot.Core.Enums;

namespace CafeBot.Core.Entities;

public class Room
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? Number { get; set; }
    public int Capacity { get; set; }
    public string? Description { get; set; }
    public string? PhotoUrl { get; set; }
    public RoomStatus Status { get; set; } = RoomStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Навигационные свойства
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    
    // Бизнес-логика
    public bool IsAvailable(DateTime date, TimeSlot timeSlot)
    {
        if (Status != RoomStatus.Active)
            return false;
            
        return !Orders.Any(o => 
            o.BookingDate.Date == date.Date &&
            o.TimeSlot == timeSlot &&
            o.Status != OrderStatus.Cancelled &&
            o.Status != OrderStatus.Completed);
    }
}