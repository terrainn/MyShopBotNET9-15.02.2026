using Telegram.Bot;
using MyShopBotNET9.Models;

namespace MyShopBotNET9.Services;

public class NotificationService
{
    private readonly ITelegramBotClient _botClient;

    public NotificationService(ITelegramBotClient botClient)
    {
        _botClient = botClient;
    }

    public async Task NotifyUserAsync(long userId, string message)
    {
        try
        {
            await _botClient.SendTextMessageAsync(
                chatId: userId, // Используем userId как chatId
                text: message);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error notifying user {userId}: {ex.Message}");
        }
    }

    public async Task NotifyAdminsAsync(string message, List<User> admins)
    {
        try
        {
            foreach (var admin in admins.Where(a => a.IsAdmin))
            {
                await _botClient.SendTextMessageAsync(
                    chatId: admin.Id, // Используем admin.Id как chatId
                    text: message);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error notifying admins: {ex.Message}");
        }
    }

    public async Task NotifyOrderStatusAsync(long userId, string orderInfo)
    {
        try
        {
            await _botClient.SendTextMessageAsync(
                chatId: userId,
                text: $"📦 Статус заказа обновлен:\n\n{orderInfo}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error notifying order status: {ex.Message}");
        }
    }
}