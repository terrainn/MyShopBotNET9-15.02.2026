using Telegram.Bot;
using Telegram.Bot.Types;
using MyShopBotNET9.Models;
using MyShopBotNET9.Services;
using MyShopBotNET9.Handlers.Interfaces;
using MyShopBotNET9.Keyboards;
using MyUser = MyShopBotNET9.Models.User;

namespace MyShopBotNET9.Handlers.MessageHandlers;

public class AdminMessageHandler : IMessageHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly AdminService _adminService;
    private readonly UserService _userService;

    public AdminMessageHandler(ITelegramBotClient botClient, AdminService adminService, UserService userService)
    {
        _botClient = botClient;
        _adminService = adminService;
        _userService = userService;
    }

    public bool CanHandle(string message, BotState userState)
    {
        // Только команды админки
        return message == "⚙️ Админка" || message == "/admin";
    }

    public async Task HandleAsync(Message message, MyUser user, CancellationToken ct)
    {
        // ВАЖНО: Двойная проверка
        // 1. Проверка в объекте пользователя (из памяти)
        // 2. Проверка в базе данных (надежный источник)

        if (!user.IsAdmin)
        {
            Console.WriteLine($"⚠️ Попытка доступа к админке от не-админа: {user.Id}");
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "❌ У вас нет доступа к админ-панели",
                cancellationToken: ct);
            return;
        }

        // ДОПОЛНИТЕЛЬНАЯ проверка в базе данных
        var userFromDb = await _userService.GetUserAsync(user.Id);
        if (userFromDb == null || !userFromDb.IsAdmin)
        {
            Console.WriteLine($"🚨 СЕРЬЕЗНО: Попытка обхода защиты админки! User ID: {user.Id}");
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "🚨 Доступ запрещен",
                cancellationToken: ct);
            return;
        }

        // Если обе проверки пройдены
        await _userService.UpdateUserStateAsync(user.Id, BotState.AdminPanel);

        var stats = await _adminService.GetStatsAsync();

        var statsText = $"📊 **Админ-панель**\n\n" +
                       $"👥 Пользователей: {stats.TotalUsers}\n" +
                       $"📦 Заказов: {stats.TotalOrders}\n" +
                       $"🎁 Товаров: {stats.TotalProducts}\n" +
                       $"💰 Выручка: {stats.TotalRevenue}₽\n" +
                       $"⏳ Ожидающих заказов: {stats.PendingOrders}";

        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: statsText,
            replyMarkup: AdminKeyboards.GetAdminMainKeyboard(),
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            cancellationToken: ct);
    }
}