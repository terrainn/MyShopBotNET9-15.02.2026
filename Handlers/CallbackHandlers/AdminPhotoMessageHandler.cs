using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using MyShopBotNET9.Models;
using MyShopBotNET9.Services;
using MyShopBotNET9.Data;
using MyShopBotNET9.Handlers.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.IO;
using MyUser = MyShopBotNET9.Models.User;

namespace MyShopBotNET9.Handlers.MessageHandlers;

public class AdminPhotoMessageHandler : IMessageHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly AppDbContext _context;
    private readonly IAdminStateService _adminStateService;
    private readonly UserService _userService;
    private readonly NotificationService _notificationService;

    private static readonly Dictionary<long, PhotoData> _pendingPhotos = new();

    public AdminPhotoMessageHandler(
        ITelegramBotClient botClient,
        AppDbContext context,
        IAdminStateService adminStateService,
        UserService userService,
        NotificationService notificationService)
    {
        _botClient = botClient;
        _context = context;
        _adminStateService = adminStateService;
        _userService = userService;
        _notificationService = notificationService;

        Console.WriteLine($"✅ AdminPhotoMessageHandler создан");
    }

    public bool CanHandle(string text, BotState state) =>
        state == BotState.AdminWaitingForProductPhoto ||
        state == BotState.AdminWaitingForOrderComment;

    public async Task HandleAsync(Message message, MyUser user, CancellationToken ct)
    {
        Console.WriteLine($"📸 AdminPhotoMessageHandler: обработка от пользователя {user.Id}, состояние={user.CurrentState}");

        if (!AdminConfig.IsAdmin(user.Id))
        {
            Console.WriteLine($"🚨 СЕРЬЕЗНОЕ НАРУШЕНИЕ БЕЗОПАСНОСТИ: Не-админ пытается отправить фото! User ID: {user.Id}");

            await _botClient.SendTextMessageAsync(
                message.Chat.Id,
                "🚨 Системная ошибка безопасности. Действие заблокировано.",
                cancellationToken: ct);

            LogSecurityViolation(user.Id, "Attempt to send delivery photo without admin rights");
            return;
        }

        int? orderId = _adminStateService.GetEditingProductId(user.Id);
        if (orderId == null)
        {
            Console.WriteLine($"⚠️ Ошибка: не найден ID заказа для пользователя {user.Id}");
            await _botClient.SendTextMessageAsync(
                message.Chat.Id,
                "❌ Ошибка: не найден заказ. Начните процесс заново.",
                cancellationToken: ct);
            return;
        }

        if (user.CurrentState == BotState.AdminWaitingForProductPhoto)
        {
            await HandlePhotoUploadAsync(message, user, orderId.Value, ct);
        }
        else if (user.CurrentState == BotState.AdminWaitingForOrderComment)
        {
            await HandleCommentUploadAsync(message, user, orderId.Value, ct);
        }
    }

    private async Task HandlePhotoUploadAsync(Message message, MyUser user, int orderId, CancellationToken ct)
    {
        if (message.Photo == null || message.Photo.Length == 0)
        {
            Console.WriteLine($"⚠️ Пользователь {user.Id} отправил сообщение без фото");
            await _botClient.SendTextMessageAsync(
                message.Chat.Id,
                "❌ Пожалуйста, отправьте изображение (не файл).",
                cancellationToken: ct);
            return;
        }

        var order = await GetOrderWithSecurityCheckAsync(orderId, user.Id, ct);
        if (order == null) return;

        try
        {
            var photo = message.Photo.Last();
            string fileId = photo.FileId;

            var file = await _botClient.GetFileAsync(fileId, ct);
            Console.WriteLine($"📁 File info for order {order.Id}: ID={fileId}, Size={file.FileSize}");

            _pendingPhotos[user.Id] = new PhotoData
            {
                OrderId = orderId,
                FileId = fileId,
                ReceivedAt = DateTime.UtcNow
            };

            await _userService.UpdateUserStateAsync(user.Id, BotState.AdminWaitingForOrderComment);

            Console.WriteLine($"✅ Фото получено для заказа {order.Id}, ожидаем комментарий");

            await _botClient.SendTextMessageAsync(
                message.Chat.Id,
                $"📸 **Фото получено для заказа №{order.Id}**\n\n" +
                "📝 Теперь введите комментарий к заказу (например, 'Товар доставлен в целости' или 'Оплата получена'):\n\n" +
                "Можно просто написать 'ОК', если комментарий не требуется.",
                cancellationToken: ct);

        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Критическая ошибка обработки фото для заказа {orderId}: {ex.Message}");
            Console.WriteLine($"🔍 Stack trace: {ex.StackTrace}");

            await _botClient.SendTextMessageAsync(
                message.Chat.Id,
                $"❌ Ошибка при загрузке фото: {ex.Message}",
                cancellationToken: ct);

            LogSecurityViolation(user.Id, $"Error processing photo: {ex.Message}");
        }
    }

    private async Task HandleCommentUploadAsync(Message message, MyUser user, int orderId, CancellationToken ct)
    {
        if (!_pendingPhotos.TryGetValue(user.Id, out var photoData) || photoData.OrderId != orderId)
        {
            Console.WriteLine($"⚠️ Фото не найдено для пользователя {user.Id} и заказа {orderId}");
            await _botClient.SendTextMessageAsync(
                message.Chat.Id,
                "❌ Фото не найдено. Начните процесс заново.",
                cancellationToken: ct);
            return;
        }

        var order = await GetOrderWithSecurityCheckAsync(orderId, user.Id, ct);
        if (order == null) return;

        try
        {
            order.DeliveryPhotoUrl = photoData.FileId;
            order.DeliveryComment = message.Text?.Trim();

            if (string.IsNullOrEmpty(order.DeliveryComment))
            {
                order.DeliveryComment = "Без комментария";
            }

            await _context.SaveChangesAsync(ct);

            Console.WriteLine($"✅ Фото и комментарий сохранены для заказа {order.Id}");

            LogAdminAction(user.Id, $"Sent delivery photo and comment for order {order.Id}");

            bool notificationSent = await SendDeliveryNotificationAsync(order, photoData.FileId, ct);

            await _botClient.SendTextMessageAsync(
                message.Chat.Id,
                $"✅ **Фото и комментарий успешно отправлены клиенту!**\n\n" +
                $"📦 Заказ №{order.Id}\n" +
                $"📸 Фото: сохранено\n" +
                $"💬 Комментарий: {order.DeliveryComment}\n\n" +
                (notificationSent ? "✅ Клиент получил уведомление" : "⚠️ Клиент получил текстовое уведомление"),
                cancellationToken: ct);

        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Ошибка сохранения комментария для заказа {orderId}: {ex.Message}");

            await _botClient.SendTextMessageAsync(
                message.Chat.Id,
                $"❌ Ошибка при сохранении комментария: {ex.Message}",
                cancellationToken: ct);

            LogSecurityViolation(user.Id, $"Error saving comment: {ex.Message}");
        }
        finally
        {
            _pendingPhotos.Remove(user.Id);

            _adminStateService.ClearEditingState(user.Id);
            await _userService.UpdateUserStateAsync(user.Id, BotState.AdminPanel);

            Console.WriteLine($"🔄 Состояние пользователя {user.Id} сброшено");
        }
    }

    private async Task<bool> IsUserReallyAdminAsync(long userId)
    {
        try
        {
            var userFromDb = await _userService.GetUserAsync(userId);
            if (userFromDb == null)
            {
                Console.WriteLine($"🔍 User {userId} not found in database during admin check");
                return false;
            }

            bool isAdmin = userFromDb.IsAdmin;
            Console.WriteLine($"🔍 Admin verification for user {userId}: {isAdmin}");
            return isAdmin;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Database error verifying admin status for user {userId}: {ex.Message}");
            return false;
        }
    }

    private async Task<Order?> GetOrderWithSecurityCheckAsync(int orderId, long adminId, CancellationToken ct)
    {
        try
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId, ct);

            if (order == null)
            {
                Console.WriteLine($"⚠️ Заказ {orderId} не найден в БД (запросил админ {adminId})");
                await _botClient.SendTextMessageAsync(
                    chatId: adminId,
                    text: "❌ Заказ не найден в базе данных",
                    cancellationToken: ct);
                return null;
            }

            Console.WriteLine($"🔍 Security check passed for order {orderId}, requested by admin {adminId}");
            return order;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Database error getting order {orderId}: {ex.Message}");
            await _botClient.SendTextMessageAsync(
                chatId: adminId,
                text: "❌ Ошибка базы данных при получении заказа",
                cancellationToken: ct);
            return null;
        }
    }

    private async Task<bool> SendDeliveryNotificationAsync(Order order, string fileId, CancellationToken ct)
    {
        try
        {
            var orderItemsText = string.Join("\n", order.OrderItems.Select(i =>
                $"• {i.ProductName} x{i.Quantity}"));

            var caption = $"🎉 **Ваш заказ №{order.Id} доставлен!**\n\n" +
                         $"💰 Сумма: {order.TotalAmount}₽\n" +
                         $"📍 Адрес: {order.Address}\n" +
                         $"💬 Комментарий от продавца: {order.DeliveryComment}\n" +
                         $"📦 Товары:\n{orderItemsText}\n\n" +
                         $"📸 **Фото доставленного товара:**";

            Console.WriteLine($"📤 Attempting to send photo to client {order.UserId} for order {order.Id}");

            await _botClient.SendPhotoAsync(
                chatId: order.UserId,
                photo: InputFile.FromFileId(fileId),
                caption: caption,
                parseMode: ParseMode.Markdown,
                cancellationToken: ct);

            Console.WriteLine($"✅ Photo sent successfully to client {order.UserId}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error sending delivery photo to user {order.UserId}: {ex.Message}");

            try
            {
                Console.WriteLine($"🔄 Fallback: sending text-only notification to client {order.UserId}");

                await _botClient.SendTextMessageAsync(
                    chatId: order.UserId,
                    text: $"🎉 **Ваш заказ №{order.Id} доставлен!**\n\n" +
                          $"💰 Сумма: {order.TotalAmount}₽\n" +
                          $"📍 Адрес: {order.Address}\n" +
                          $"💬 Комментарий от продавца: {order.DeliveryComment}\n" +
                          $"📦 Товары:\n{string.Join("\n", order.OrderItems.Select(i => $"• {i.ProductName} x{i.Quantity}"))}\n\n" +
                          "📸 Администратор загрузил фото доставки. " +
                          "Если фото не отобразилось, обратитесь в поддержку для получения фото.",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: ct);

                Console.WriteLine($"✅ Fallback text notification sent to client {order.UserId}");
                return false;
            }
            catch (Exception innerEx)
            {
                Console.WriteLine($"❌ CRITICAL: Error sending fallback notification to client {order.UserId}: {innerEx.Message}");
                return false;
            }
        }
    }

    private void LogSecurityViolation(long userId, string violationDetails)
    {
        try
        {
            string logMessage = $"[SECURITY VIOLATION - PHOTO HANDLER] {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} - " +
                               $"User ID: {userId} - {violationDetails}";

            Console.WriteLine($"🚨 {logMessage}");
            System.IO.File.AppendAllText("security_photo_violations.log", logMessage + Environment.NewLine);
            System.IO.File.AppendAllText("security_violations.log", logMessage + Environment.NewLine);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error logging security violation in photo handler: {ex.Message}");
        }
    }

    private void LogAdminAction(long adminId, string action)
    {
        try
        {
            string logMessage = $"[ADMIN ACTION - PHOTO] {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} - " +
                               $"Admin ID: {adminId} - {action}";

            Console.WriteLine($"📝 {logMessage}");
            System.IO.File.AppendAllText("admin_photo_actions.log", logMessage + Environment.NewLine);
            System.IO.File.AppendAllText("admin_actions.log", logMessage + Environment.NewLine);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error logging admin photo action: {ex.Message}");
        }
    }

    private class PhotoData
    {
        public int OrderId { get; set; }
        public string FileId { get; set; } = string.Empty;
        public DateTime ReceivedAt { get; set; }
    }
}