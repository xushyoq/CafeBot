using CafeBot.Core.Enums;

namespace CafeBot.Core.Entities;

public class Employee
{
    public int Id { get; set; }
    public long TelegramId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public EmployeeRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; // Добавлено свойство UpdatedAt
    public DateTime? LastActiveAt { get; set; }
    
    // Навигационные свойства
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Payment> ReceivedPayments { get; set; } = new List<Payment>();
}