public enum BotState
{
    MainMenu,
    Catalog,
    Cart,
    Profile,
    AdminPanel,

    // Поля для оформления заказа
    WaitingForDistrict,
    WaitingForAddress,
    WaitingForPhone,
    WaitingForPayment,

    // Админские поля
    AdminWaitingForProductName,
    AdminWaitingForProductPrice,
    AdminWaitingForProductDescription,
    AdminWaitingForProductCategory,
    AdminWaitingForProductCity,
    AdminWaitingForProductPhoto,
    AdminWaitingForProductStock,
    AdminWaitingForProductGramPrices,
    AdminWaitingForDeliveryTime,
    AdminWaitingForOrderComment,

    // Поддержка
    WaitingForSupportOrderSelection,  // Выбор заказа для обращения
    WaitingForSupportMessage,         // Ожидание сообщения от клиента
    AdminReplyingToSupport            // Админ отвечает в поддержку
}