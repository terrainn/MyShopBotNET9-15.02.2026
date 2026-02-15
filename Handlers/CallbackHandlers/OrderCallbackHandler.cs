using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using MyShopBotNET9.Models;
using MyShopBotNET9.Services;
using MyShopBotNET9.Handlers.Interfaces;
using MyShopBotNET9.Data;
using MyShopBotNET9.Keyboards;
using Microsoft.EntityFrameworkCore;
using MyUser = MyShopBotNET9.Models.User;
using Microsoft.Extensions.DependencyInjection;

namespace MyShopBotNET9.Handlers.CallbackHandlers;

public class OrderCallbackHandler : ICallbackHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly AppDbContext _context;
    private readonly UserService _userService;
    private readonly SupportService _supportService;
    private readonly IServiceProvider _serviceProvider;

    public OrderCallbackHandler(
        ITelegramBotClient botClient,
        AppDbContext context,
        UserService userService,
        IServiceProvider serviceProvider)
    {
        _botClient = botClient;
        _context = context;
        _userService = userService;
        _serviceProvider = serviceProvider;
        _supportService = serviceProvider.GetRequiredService<SupportService>();
    }

    public bool CanHandle(string callbackData) =>
        !string.IsNullOrEmpty(callbackData) && (
            callbackData == "my_orders" ||
            callbackData == "show_orders" ||
            callbackData.StartsWith("order_details_") ||
            callbackData.StartsWith("support_order_") ||
            callbackData.StartsWith("support_history_")
        );

    public async Task HandleAsync(CallbackQuery callback, MyUser user, CancellationToken ct)
    {
        if (callback.Message == null || string.IsNullOrEmpty(callback.Data)) return;

        var data = callback.Data;

        try
        {
            if (data == "my_orders" || data == "show_orders")
            {
                await ShowMyOrdersAsync(callback.Message.Chat.Id, callback.Message.MessageId, user.Id, ct);
            }
            else if (data.StartsWith("order_details_"))
            {
                if (int.TryParse(data.Replace("order_details_", ""), out int orderId))
                {
                    await ShowOrderDetailAsync(callback.Message.Chat.Id, callback.Message.MessageId, orderId, user.Id, ct);
                }
            }
            else if (data.StartsWith("support_order_"))
            {
                if (int.TryParse(data.Replace("support_order_", ""), out int orderId))
                {
                    await StartSupportChatAsync(callback, user, orderId, ct);
                }
            }
            else if (data.StartsWith("support_history_"))
            {
                if (int.TryParse(data.Replace("support_history_", ""), out int orderId))
                {
                    await ShowSupportHistoryAsync(callback.Message.Chat.Id, callback.Message.MessageId, orderId, user.Id, ct);
                }
            }

            await _botClient.AnswerCallbackQueryAsync(callback.Id, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error in OrderCallbackHandler: {ex.Message}");
            await _botClient.AnswerCallbackQueryAsync(callback.Id, "⚠️ Произошла ошибка", cancellationToken: ct);
        }
    }

    private async Task ShowMyOrdersAsync(long chatId, int messageId, long userId, CancellationToken ct)
    {
        var orders = await _context.Orders
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.OrderDate)
            .Take(10)
            .ToListAsync(ct);

        if (!orders.Any())
        {
            await _botClient.EditMessageTextAsync(
                chatId: chatId,
                messageId: messageId,
                text: "📭 У вас пока нет заказов.",
                replyMarkup: InlineKeyboards.GetEmptyOrdersKeyboard(),
                cancellationToken: ct);
            return;
        }

        var buttons = orders.Select(o => new[] {
            InlineKeyboardButton.WithCallbackData(
                $"📦 №{o.Id} от {o.OrderDate:dd.MM} - {o.TotalAmount}₽",
                $"order_details_{o.Id}")
        }).ToList();

        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Назад в меню", "main_menu") });

        await _botClient.EditMessageTextAsync(
            chatId: chatId,
            messageId: messageId,
            text: "📜 **Ваши последние заказы:**",
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            replyMarkup: new InlineKeyboardMarkup(buttons),
            cancellationToken: ct);
    }

    private async Task ShowOrderDetailAsync(long chatId, int messageId, int orderId, long userId, CancellationToken ct)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId, ct);

        if (order == null)
        {
            await _botClient.EditMessageTextAsync(
                chatId: chatId,
                messageId: messageId,
                text: "❌ Заказ не найден",
                cancellationToken: ct);
            return;
        }

        // Проверяем, есть ли непрочитанные сообщения от админа
        var hasUnreadMessages = await _supportService.HasUnreadMessagesForClientAsync(userId);

        string details = $"🆔 **Заказ №{order.Id}**\n" +
                        $"──────────────────\n" +
                        $"💰 **Сумма:** {order.TotalAmount}₽\n" +
                        $"📍 **Адрес:** {order.Address ?? "Не указан"}\n" +
                        $"📊 **Статус:** {GetOrderStatusEmoji(order.Status)} {order.Status}\n" +
                        $"📅 **Дата:** {order.OrderDate:dd.MM.yyyy HH:mm}\n\n" +
                        $"🛒 **Товары:**\n" +
                        string.Join("\n", order.OrderItems.Select(i => $"• {i.ProductName} x{i.Quantity} - {i.Price}₽"));

        var keyboard = InlineKeyboards.GetOrderDetailsKeyboard(orderId, order.Status, hasUnreadMessages);

        await _botClient.EditMessageTextAsync(
            chatId: chatId,
            messageId: messageId,
            text: details,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            replyMarkup: keyboard,
            cancellationToken: ct);
    }

    private async Task StartSupportChatAsync(CallbackQuery callback, MyUser user, int orderId, CancellationToken ct)
    {
        // Проверяем, что заказ принадлежит пользователю
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == user.Id, ct);

        if (order == null)
        {
            await _botClient.AnswerCallbackQueryAsync(callback.Id, "❌ Заказ не найден", cancellationToken: ct);
            return;
        }

        // Отмечаем сообщения как прочитанные
        await _supportService.MarkMessagesAsReadByClientAsync(orderId, user.Id);

        // Показываем историю переписки
        await ShowSupportHistoryAsync(callback.Message!.Chat.Id, callback.Message.MessageId, orderId, user.Id, ct);

        // Устанавливаем состояние ожидания сообщения
        await _userService.UpdateUserStateAsync(user.Id, BotState.WaitingForSupportMessage);

        // Сохраняем ID заказа во временном состоянии (используем AdminStateService как временное хранилище)
        using var scope = _serviceProvider.CreateScope();
        var adminStateService = scope.ServiceProvider.GetRequiredService<IAdminStateService>();
        adminStateService.SetEditingProductId(user.Id, orderId);

        await _botClient.SendTextMessageAsync(
            chatId: callback.Message.Chat.Id,
            text: $"💬 **Чат с поддержкой по заказу №{orderId}**\n\n" +
                  "Напишите ваше сообщение, и мы ответим в ближайшее время.\n" +
                  "Вы также можете отправить фото.",
            replyMarkup: InlineKeyboards.GetSupportChatKeyboard(orderId),
            cancellationToken: ct);
    }

    private async Task ShowSupportHistoryAsync(long chatId, int messageId, int orderId, long userId, CancellationToken ct)
    {
        var messages = await _supportService.GetOrderChatHistoryAsync(orderId);

        if (!messages.Any())
        {
            await _botClient.EditMessageTextAsync(
                chatId: chatId,
                messageId: messageId,
                text: $"💬 **Чат с поддержкой по заказу №{orderId}**\n\n" +
                      "Пока нет сообщений. Напишите что-нибудь!",
                replyMarkup: InlineKeyboards.GetSupportChatKeyboard(orderId),
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                cancellationToken: ct);
            return;
        }

        var chatHistory = $"💬 **Чат с поддержкой по заказу №{orderId}**\n\n";

        foreach (var msg in messages.OrderBy(m => m.SentAt))
        {
            var sender = msg.SenderType == SenderType.Client ? "👤 Вы" : "👨‍💼 Поддержка";
            var time = msg.SentAt.ToString("HH:mm");

            if (!string.IsNullOrEmpty(msg.PhotoFileId))
            {
                chatHistory += $"{sender} [{time}]: 📸 Фото\n";
            }
            else if (!string.IsNullOrEmpty(msg.MessageText))
            {
                // Обрезаем длинные сообщения для истории
                var text = msg.MessageText.Length > 50
                    ? msg.MessageText.Substring(0, 47) + "..."
                    : msg.MessageText;
                chatHistory += $"{sender} [{time}]: {text}\n";
            }
        }

        chatHistory += "\n📝 Напишите сообщение ниже, чтобы продолжить диалог.";

        await _botClient.EditMessageTextAsync(
            chatId: chatId,
            messageId: messageId,
            text: chatHistory,
            replyMarkup: InlineKeyboards.GetSupportChatKeyboard(orderId),
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            cancellationToken: ct);
    }

    private string GetOrderStatusEmoji(OrderStatus status)
    {
        return status switch
        {
            OrderStatus.Pending => "⏳",
            OrderStatus.Confirmed => "✅",
            OrderStatus.Shipped => "🚚",
            OrderStatus.Delivered => "🎁",
            OrderStatus.Completed => "🏁",
            OrderStatus.Cancelled => "❌",
            _ => "📝"
        };
    }
}