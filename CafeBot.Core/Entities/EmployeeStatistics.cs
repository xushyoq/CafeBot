namespace CafeBot.Core.Entities;

public class EmployeeStatistics
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int OrdersCount { get; set; }        // Количество завершенных заказов
    public decimal TotalRevenue { get; set; }   // Общая сумма выручки
    public string Period { get; set; } = string.Empty;
}




