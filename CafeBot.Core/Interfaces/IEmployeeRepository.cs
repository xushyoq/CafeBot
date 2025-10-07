using CafeBot.Core.Entities;

namespace CafeBot.Core.Interfaces;

public interface IEmployeeRepository : IRepository<Employee>
{
    Task<Employee?> GetByTelegramIdAsync(long telegramId);
    Task<IEnumerable<Employee>> GetActiveEmployeesAsync();
    Task<IEnumerable<Employee>> GetByRoleAsync(Enums.EmployeeRole role);
}