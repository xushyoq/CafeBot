using CafeBot.Core.Enums;

namespace CafeBot.Core.Entities;

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public ProductUnit Unit { get; set; }
    public decimal Price { get; set; }
    public decimal Subtotal { get; set; }
    
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public int? AddedByEmployeeId { get; set; }
    
    // Навигационные свойства
    public Order Order { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public Employee? AddedByEmployee { get; set; }
    
    // Бизнес-логика
    public void CalculateSubtotal()
    {
        Subtotal = Quantity * Price;
    }
}