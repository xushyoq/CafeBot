using CafeBot.Core.Enums;

namespace CafeBot.Core.Entities;

public class Payment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public int ReceivedByEmployeeId { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    
    // Навигационные свойства
    public Order Order { get; set; } = null!;
    public Employee ReceivedByEmployee { get; set; } = null!;
}