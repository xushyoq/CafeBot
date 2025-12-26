using CafeBot.Core.Entities;
using CafeBot.Core.Enums;
using CafeBot.Core.Interfaces;

namespace CafeBot.Application.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IUnitOfWork _unitOfWork;

    public EmployeeService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Employee?> GetEmployeeByIdAsync(int employeeId)
    {
        return await _unitOfWork.Employees.GetByIdAsync(employeeId);
    }

    public async Task<Employee?> GetEmployeeByTelegramIdAsync(long telegramId)
    {
        return await _unitOfWork.Employees.GetByTelegramIdAsync(telegramId);
    }

    public async Task<IEnumerable<Employee>> GetAllEmployeesAsync()
    {
        return await _unitOfWork.Employees.GetAllAsync();
    }

    public async Task<IEnumerable<Employee>> GetActiveEmployeesAsync()
    {
        return await _unitOfWork.Employees.GetActiveEmployeesAsync();
    }

    public async Task<Employee> CreateEmployeeAsync(long telegramId, string firstName, string lastName, string phone, EmployeeRole role)
    {
        var employee = new Employee
        {
            TelegramId = telegramId,
            FirstName = firstName,
            LastName = lastName,
            Phone = phone,
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow, // Добавлено
            LastActiveAt = DateTime.UtcNow
        };

        await _unitOfWork.Employees.AddAsync(employee);
        await _unitOfWork.SaveChangesAsync(); // Исправлено
        return employee;
    }

    public async Task<Employee?> UpdateEmployeeAsync(int employeeId, string firstName, string lastName, string phone, EmployeeRole role, bool isActive)
    {
        var employee = await _unitOfWork.Employees.GetByIdAsync(employeeId);
        if (employee == null)
            return null;

        employee.FirstName = firstName;
        employee.LastName = lastName;
        employee.Phone = phone;
        employee.Role = role;
        employee.IsActive = isActive;
        employee.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Employees.UpdateAsync(employee); // Исправлено
        await _unitOfWork.SaveChangesAsync(); // Исправлено
        return employee;
    }

    public async Task<bool> DeactivateEmployeeAsync(int employeeId)
    {
        var employee = await _unitOfWork.Employees.GetByIdAsync(employeeId);
        if (employee == null)
            return false;

        employee.IsActive = false;
        employee.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Employees.UpdateAsync(employee); // Исправлено
        await _unitOfWork.SaveChangesAsync(); // Исправлено
        return true;
    }

    public async Task<Employee?> GetEmployeeWithOrdersAndPaymentsAsync(int employeeId)
    {
        // Необходимо реализовать в репозитории EmployeeRepository.cs
        // Этот метод должен включать связанные заказы и платежи
        // Заглушка для компиляции
        var employee = await _unitOfWork.Employees.GetByIdAsync(employeeId);
        if (employee != null)
        {
            // Добавьте логику для загрузки заказов и платежей
            // Например: employee.Orders = (await _unitOfWork.Orders.GetOrdersByEmployeeIdAsync(employeeId)).ToList();
            // employee.ReceivedPayments = (await _unitOfWork.Payments.GetPaymentsByEmployeeIdAsync(employeeId)).ToList();
        }
        return employee;
    }
}