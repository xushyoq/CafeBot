using CafeBot.Core.Interfaces;
using CafeBot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace CafeBot.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
        
        Employees = new EmployeeRepository(context);
        Rooms = new RoomRepository(context);
        Products = new ProductRepository(context);
        Categories = new CategoryRepository(context);
        Orders = new OrderRepository(context);
        OrderItems = new OrderItemRepository(context);
        Payments = new PaymentRepository(context);
    }

    public IEmployeeRepository Employees { get; }
    public IRoomRepository Rooms { get; }
    public IProductRepository Products { get; }
    public ICategoryRepository Categories { get; }
    public IOrderRepository Orders { get; }
    public IOrderItemRepository OrderItems { get; }
    public IPaymentRepository Payments { get; }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        try
        {
            await SaveChangesAsync();
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
            }
        }
        catch
        {
            await RollbackTransactionAsync();
            throw;
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}