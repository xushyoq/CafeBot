using CafeBot.Core.Entities;
using CafeBot.Core.Enums;
using CafeBot.Core.Interfaces;

namespace CafeBot.Application.Services;

public interface IOrderService
{
    Task<Order> CreateOrderAsync(int roomId, int employeeId, string clientName, string clientPhone, 
        int guestCount, DateTime bookingDate, TimeSlot timeSlot, string? notes = null);
    Task<Order?> GetOrderByIdAsync(int orderId);
    Task<Order?> GetOrderWithDetailsAsync(int orderId);
    Task<IEnumerable<Order>> GetActiveOrdersAsync();
    Task<IEnumerable<Order>> GetOrdersByDateAsync(DateTime date);
    Task<bool> AddItemToOrderAsync(int orderId, int productId, decimal quantity, int addedByEmployeeId);
    Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus);
    Task<bool> CancelOrderAsync(int orderId);
    Task<decimal> CalculateOrderTotalAsync(int orderId);
}

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;

    public OrderService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Order> CreateOrderAsync(int roomId, int employeeId, string clientName, 
        string clientPhone, int guestCount, DateTime bookingDate, TimeSlot timeSlot, string? notes = null)
    {
        // Проверяем доступность комнаты
        var isAvailable = await _unitOfWork.Orders.IsRoomAvailableAsync(roomId, bookingDate, timeSlot);
        if (!isAvailable)
        {
            throw new InvalidOperationException("Комната уже забронирована на это время");
        }

        // Проверяем существование комнаты
        var room = await _unitOfWork.Rooms.GetByIdAsync(roomId);
        if (room == null || room.Status != RoomStatus.Active)
        {
            throw new ArgumentException("Комната не найдена или не активна");
        }

        // Проверяем существование работника
        var employee = await _unitOfWork.Employees.GetByIdAsync(employeeId);
        if (employee == null || !employee.IsActive)
        {
            throw new ArgumentException("Работник не найден или не активен");
        }

        // Генерируем номер заказа
        var orderNumber = await _unitOfWork.Orders.GenerateOrderNumberAsync();

        // Создаём заказ
        var order = new Order
        {
            OrderNumber = orderNumber,
            RoomId = roomId,
            EmployeeId = employeeId,
            ClientName = clientName,
            ClientPhone = clientPhone,
            GuestCount = guestCount,
            BookingDate = bookingDate,
            TimeSlot = timeSlot,
            Status = OrderStatus.Created,
            Notes = notes,
            TotalAmount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Orders.AddAsync(order);
        await _unitOfWork.SaveChangesAsync();

        return order;
    }

    public async Task<Order?> GetOrderByIdAsync(int orderId)
    {
        return await _unitOfWork.Orders.GetByIdAsync(orderId);
    }

    public async Task<Order?> GetOrderWithDetailsAsync(int orderId)
    {
        return await _unitOfWork.Orders.GetOrderWithDetailsAsync(orderId);
    }

    public async Task<IEnumerable<Order>> GetActiveOrdersAsync()
    {
        var statuses = new[] 
        { 
            OrderStatus.Created, 
            OrderStatus.Confirmed, 
            OrderStatus.Active, 
            OrderStatus.ReadyToPay 
        };

        var allOrders = await _unitOfWork.Orders.GetAllAsync();
        return allOrders.Where(o => statuses.Contains(o.Status));
    }

    public async Task<IEnumerable<Order>> GetOrdersByDateAsync(DateTime date)
    {
        return await _unitOfWork.Orders.GetOrdersByDateAsync(date);
    }

    public async Task<bool> AddItemToOrderAsync(int orderId, int productId, decimal quantity, int addedByEmployeeId)
    {
        var order = await _unitOfWork.Orders.GetOrderWithDetailsAsync(orderId);
        if (order == null)
        {
            throw new ArgumentException("Заказ не найден");
        }

        if (!order.CanAddItems())
        {
            throw new InvalidOperationException("Нельзя добавить позиции к завершенному или отмененному заказу");
        }

        var product = await _unitOfWork.Products.GetByIdAsync(productId);
        if (product == null || !product.IsAvailable)
        {
            throw new ArgumentException("Продукт не найден или недоступен");
        }

        var orderItem = new OrderItem
        {
            OrderId = orderId,
            ProductId = productId,
            ProductName = product.Name,
            Quantity = quantity,
            Unit = product.Unit,
            Price = product.Price,
            AddedByEmployeeId = addedByEmployeeId,
            AddedAt = DateTime.UtcNow
        };

        orderItem.CalculateSubtotal();

        await _unitOfWork.OrderItems.AddAsync(orderItem);
        
        // Пересчитываем общую сумму
        order.OrderItems.Add(orderItem);
        order.CalculateTotal();
        order.UpdatedAt = DateTime.UtcNow;
        
        await _unitOfWork.UpdateAsync(order);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
        if (order == null)
        {
            return false;
        }

        order.Status = newStatus;
        order.UpdatedAt = DateTime.UtcNow;

        if (newStatus == OrderStatus.Completed)
        {
            order.CompletedAt = DateTime.UtcNow;
        }

        await _unitOfWork.Orders.UpdateAsync(order);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> CancelOrderAsync(int orderId)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
        if (order == null)
        {
            return false;
        }

        if (!order.CanBeCancelled())
        {
            throw new InvalidOperationException("Заказ нельзя отменить на текущем этапе");
        }

        order.Status = OrderStatus.Cancelled;
        order.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Orders.UpdateAsync(order);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<decimal> CalculateOrderTotalAsync(int orderId)
    {
        var order = await _unitOfWork.Orders.GetOrderWithDetailsAsync(orderId);
        if (order == null)
        {
            return 0;
        }

        order.CalculateTotal();
        return order.TotalAmount;
    }
}