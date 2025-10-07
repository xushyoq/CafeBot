using CafeBot.Core.Enums;

namespace CafeBot.Core.Entities;

public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    
    public int RoomId { get; set; }
    public int EmployeeId { get; set; }
    
    public string ClientName { get; set; } = string.Empty;
    public string ClientPhone { get; set; } = string.Empty;
    public int GuestCount { get; set; }
    
    public DateTime BookingDate { get; set; }
    public TimeSlot TimeSlot { get; set; }
    
    public OrderStatus Status { get; set; } = OrderStatus.Created;
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    
    // Навигационные свойства
    public Room Room { get; set; } = null!;
    public Employee Employee { get; set; } = null!;
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public Payment? Payment { get; set; }
    
    // Бизнес-логика
    public void CalculateTotal()
    {
        TotalAmount = OrderItems.Sum(item => item.Subtotal);
    }
    
    public bool CanBeCancelled()
    {
        return Status == OrderStatus.Created || Status == OrderStatus.Confirmed;
    }
    
    public bool CanAddItems()
    {
        return Status != OrderStatus.Completed && 
               Status != OrderStatus.Cancelled && 
               Status != OrderStatus.Paid;
    }
}