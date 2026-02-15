using Telegram.Bot;
using Telegram.Bot.Types;
using MyShopBotNET9.Models;
using MyShopBotNET9.Services;
using MyShopBotNET9.Data;
using MyShopBotNET9.Handlers.Interfaces;
using Microsoft.EntityFrameworkCore;
using MyUser = MyShopBotNET9.Models.User;

namespace MyShopBotNET9.Handlers.MessageHandlers;

public class AdminDeliveryTimeMessageHandler : IMessageHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly AppDbContext _context;
    private readonly IAdminStateService _adminStateService;
    private readonly UserService _userService;

    public AdminDeliveryTimeMessageHandler(
        ITelegramBotClient botClient,
        AppDbContext context,
        IAdminStateService adminStateService,
        UserService userService)
    {
        _botClient = botClient;
        _context = context;
        _adminStateService = adminStateService;
        _userService = userService;
    }

    public bool CanHandle(string text, BotState state) =>
        state == BotState.AdminWaitingForDeliveryTime;

    public async Task HandleAsync(Message message, MyUser admin, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(message.Text)) return;

        Console.WriteLine($"⏱️ AdminDeliveryTimeMessageHandler: {message.Text}");

        int? orderId = _adminStateService.GetEditingProductId(admin.Id);
        if (orderId == null)
        {
            await _botClient.SendTextMessageAsync(
                message.Chat.Id,
                "❌ Ошибка: не найден заказ. Начните процесс заново.",
                cancellationToken: ct);
            return;
        }

        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order == null)
        {
            await _botClient.SendTextMessageAsync(
                message.Chat.Id,
                "❌ Заказ не найден",
                cancellationToken: ct);
            return;
        }

        // Отправляем сообщение клиенту
        string deliveryTimeMessage = $"🚚 **Информация о доставке для заказа №{order.Id}**\n\n" +
                                    $" **{message.Text}**\n\n" +
                                    $"Спасибо за ожидание! 🎁";

        try
        {
            await _botClient.SendTextMessageAsync(
                chatId: order.UserId,
                text: deliveryTimeMessage,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                cancellationToken: ct);

            // Логируем действие
            LogAdminAction(admin.Id, $"Sent delivery time for order {order.Id}: {message.Text}");

            // Подтверждаем админу
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"✅ **Сообщение с временем доставки отправлено клиенту!**\n\n" +
                      $"⏱️ Время: {message.Text}\n" +
                      $"📦 Заказ №{order.Id}",
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error sending delivery time to user {order.UserId}: {ex.Message}");

            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"❌ Не удалось отправить сообщение клиенту. Возможно, он заблокировал бота.\n\n" +
                      $"Текст сообщения:\n{deliveryTimeMessage}",
                cancellationToken: ct);
        }

        // Очищаем состояние
        _adminStateService.ClearEditingState(admin.Id);
        await _userService.UpdateUserStateAsync(admin.Id, BotState.AdminPanel);
    }

    private void LogAdminAction(long adminId, string action)
    {
        try
        {
            string logMessage = $"[ADMIN ACTION] {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} - Admin ID: {adminId} - {action}";
            Console.WriteLine($"📝 {logMessage}");
            System.IO.File.AppendAllText("admin_actions.log", logMessage + Environment.NewLine);
        }
        catch { }
    }
}