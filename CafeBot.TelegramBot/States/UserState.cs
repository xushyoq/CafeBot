namespace CafeBot.TelegramBot.States;

public enum UserState
{
    None,
    
    // Создание заказа
    SelectingDate,
    SelectingTimeSlot,
    SelectingRoom,
    EnteringClientName,
    EnteringClientPhone,
    EnteringGuestCount,
    SelectingCategory,
    SelectingProduct,
    EnteringQuantity,
    ConfirmingOrder,
    
    // Работа с заказом
    ViewingOrderDetails,
    AddingItemsToExistingOrder,
    ProcessingPayment,

    // Администрирование - Управление сотрудниками
    AdminAddingEmployeeTelegramId,
    AdminAddingEmployeeFirstName,
    AdminAddingEmployeeLastName,
    AdminAddingEmployeePhone,
    AdminSelectingEmployeeRole,

    // Администрирование - Статистика
    AdminSelectingStatisticsStartDate,
    AdminSelectingStatisticsEndDate,

    // Администрирование - Управление категориями
    AdminAddingCategoryName,
    AdminAddingCategoryDisplayOrder,

    // Администрирование - Управление продуктами
    AdminAddingProductCategory,
    AdminAddingProductName,
    AdminAddingProductDescription,
    AdminAddingProductPrice,
    AdminAddingProductUnit,
    AdminAddingProductPhotoUrl,
    AdminAddingProductDisplayOrder,

    // Администрирование - Управление комнатами
    AdminAddingRoomName,
    AdminAddingRoomNumber,
    AdminAddingRoomCapacity,
    AdminAddingRoomDescription,
    AdminAddingRoomPhotoUrl
}