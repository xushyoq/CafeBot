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
    AdminSelectingEmployeeRole
}