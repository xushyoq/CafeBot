using CafeBot.Core.Enums;

namespace CafeBot.Core.Entities;

public class EmployeeCurrentStatus
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string Status { get; set; } = "Свободен"; // "Комната X (День)" или "Свободен"
    public int? CurrentOrderId { get; set; }
    public string? RoomName { get; set; }
    public TimeSlot? TimeSlot { get; set; }
}




