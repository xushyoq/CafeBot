using CafeBot.Core.Enums;

namespace CafeBot.Core.Entities;

public class EmployeeCurrentStatus
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string Status { get; set; } = "Bo'sh"; // "Комната X (День)" или "Bo'sh"
    public int? CurrentOrderId { get; set; }
    public string? RoomName { get; set; }
    public TimeSlot? TimeSlot { get; set; }
}




