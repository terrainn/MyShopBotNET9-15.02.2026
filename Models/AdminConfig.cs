namespace MyShopBotNET9.Models;

public static class AdminConfig
{
    // Список разрешенных администраторов по Telegram ID
    public static readonly HashSet<long> AdminUserIds = new()
    {
        382747398,   // Замените на ваш реальный ID
        987654321    // Добавьте дополнительные ID при необходимости
    };

    // Проверка, является ли пользователь админом
    public static bool IsAdmin(long userId) => AdminUserIds.Contains(userId);
}