using CafeBot.Core.Enums;

namespace CafeBot.TelegramBot.States;

public class UserStateData
{
    // Данные для создания заказа
    public DateTime? SelectedDate { get; set; }
    public TimeSlot? SelectedTimeSlot { get; set; }
    public int? SelectedRoomId { get; set; }
    public string? ClientName { get; set; }
    public string? ClientPhone { get; set; }
    public int? GuestCount { get; set; }
    
    // Для добавления продуктов
    public int? SelectedCategoryId { get; set; }
    public int? SelectedProductId { get; set; }
    public decimal? Quantity { get; set; }
    
    // Текущий заказ
    public int? CurrentOrderId { get; set; }
    
    // Временное хранилище для корзины (до создания заказа)
    public List<OrderItemData> Cart { get; set; } = new();

    // Данные для администрирования - Управление сотрудниками
    public long? AdminEmployeeTelegramId { get; set; }
    public string? AdminEmployeeFirstName { get; set; }
    public string? AdminEmployeeLastName { get; set; }
    public string? AdminEmployeePhone { get; set; }
    public EmployeeRole? AdminEmployeeRole { get; set; }

    // Данные для администрирования - Статистика
    public DateTime? AdminStatisticsStartDate { get; set; }
    public DateTime? AdminStatisticsEndDate { get; set; }

    // Данные для администрирования - Управление категориями
    public string? AdminCategoryName { get; set; }
    public int? AdminCategoryDisplayOrder { get; set; }

    // Данные для администрирования - Управление продуктами
    public int? AdminProductCategoryId { get; set; }
    public string? AdminProductName { get; set; }
    public string? AdminProductDescription { get; set; }
    public decimal? AdminProductPrice { get; set; }
    public ProductUnit? AdminProductUnit { get; set; }
    public string? AdminProductPhotoUrl { get; set; }
    public int? AdminProductDisplayOrder { get; set; }

    // Данные для администрирования - Управление комнатами
    public string? AdminRoomName { get; set; }
    public int? AdminRoomNumber { get; set; }
    public int? AdminRoomCapacity { get; set; }
    public string? AdminRoomDescription { get; set; }
    public string? AdminRoomPhotoUrl { get; set; }

    public void Clear()
    {
        SelectedDate = null;
        SelectedTimeSlot = null;
        SelectedRoomId = null;
        ClientName = null;
        ClientPhone = null;
        GuestCount = null;
        SelectedCategoryId = null;
        SelectedProductId = null;
        Quantity = null;
        CurrentOrderId = null;
        Cart.Clear();

        // Очистка данных администратора
        AdminEmployeeTelegramId = null;
        AdminEmployeeFirstName = null;
        AdminEmployeeLastName = null;
        AdminEmployeePhone = null;
        AdminEmployeeRole = null;

        // Очистка данных статистики
        AdminStatisticsStartDate = null;
        AdminStatisticsEndDate = null;

        // Очистка данных управления категориями
        AdminCategoryName = null;
        AdminCategoryDisplayOrder = null;

        // Очистка данных управления продуктами
        AdminProductCategoryId = null;
        AdminProductName = null;
        AdminProductDescription = null;
        AdminProductPrice = null;
        AdminProductUnit = null;
        AdminProductPhotoUrl = null;
        AdminProductDisplayOrder = null;

        // Очистка данных управления комнатами
        AdminRoomName = null;
        AdminRoomNumber = null;
        AdminRoomCapacity = null;
        AdminRoomDescription = null;
        AdminRoomPhotoUrl = null;
    }
}

public class OrderItemData
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public ProductUnit Unit { get; set; }
    public decimal Price { get; set; }
    public decimal Subtotal { get; set; }
}