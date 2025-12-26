using CafeBot.Core.Entities;
using CafeBot.Core.Enums;

namespace CafeBot.Application.Services;

public interface IEmployeeService
{
    Task<Employee?> GetEmployeeByIdAsync(int employeeId);
    Task<Employee?> GetEmployeeByTelegramIdAsync(long telegramId);
    Task<IEnumerable<Employee>> GetAllEmployeesAsync();
    Task<IEnumerable<Employee>> GetActiveEmployeesAsync();
    Task<Employee> CreateEmployeeAsync(long telegramId, string firstName, string lastName, string phone, EmployeeRole role);
    Task<Employee?> UpdateEmployeeAsync(int employeeId, string firstName, string lastName, string phone, EmployeeRole role, bool isActive);
    Task<bool> DeactivateEmployeeAsync(int employeeId);
    Task<Employee?> GetEmployeeWithOrdersAndPaymentsAsync(int employeeId);
}
