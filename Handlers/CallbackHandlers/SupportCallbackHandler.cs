using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using MyShopBotNET9.Models;
using MyShopBotNET9.Services;
using MyShopBotNET9.Handlers.Interfaces;
using MyShopBotNET9.Keyboards;
using MyUser = MyShopBotNET9.Models.User;

namespace MyShopBotNET9.Handlers.CallbackHandlers;

public class SupportCallbackHandler : ICallbackHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly UserService _userService;
    private readonly OrderService _orderService;
    private readonly SupportService _supportService;
    private readonly IAdminStateService _adminStateService;

    public SupportCallbackHandler(
        ITelegramBotClient botClient,
        UserService userService,
        OrderService orderService,
        SupportService supportService,
        IAdminStateService adminStateService)
    {
        _botClient = botClient;
        _userService = userService;
        _orderService = orderService;
        _supportService = supportService;
        _adminStateService = adminStateService;
    }

    public bool CanHandle(string callbackData) =>
        !string.IsNullOrEmpty(callbackData) && (
            callbackData == "support_start" ||
            callbackData.StartsWith("admin_reply_support_") ||
            callbackData.StartsWith("admin_reply_photo_") ||
            callbackData.StartsWith("admin_support_history_")
        );

    public async Task HandleAsync(CallbackQuery callback, MyUser user, CancellationToken ct)
    {
        if (callback.Message == null || string.IsNullOrEmpty(callback.Data)) return;

        var data = callback.Data;

        try
        {
            if (data == "support_start")
            {
                await HandleSupportStartAsync(callback, user, ct);
            }
            else if (data.StartsWith("admin_reply_support_"))
            {
                var parts = data.Split('_');
                // admin_reply_support_123_456
                if (parts.Length >= 5 &&
                    int.TryParse(parts[3], out int orderId) &&
                    long.TryParse(parts[4], out long clientId))
                {
                    await StartAdminReplyAsync(callback, user, orderId, clientId, false, ct);
                }
            }
            else if (data.StartsWith("admin_reply_photo_"))
            {
                var parts = data.Split('_');
                // admin_reply_photo_123_456
                if (parts.Length >= 5 &&
                    int.TryParse(parts[3], out int orderId) &&
                    long.TryParse(parts[4], out long clientId))
                {
                    await StartAdminReplyAsync(callback, user, orderId, clientId, true, ct);
                }
            }
            else if (data.StartsWith("admin_support_history_"))
            {
                if (int.TryParse(data.Replace("admin_support_history_", ""), out int orderId))
                {
                    await ShowAdminSupportHistoryAsync(callback, orderId, ct);
                }
            }

            await _botClient.AnswerCallbackQueryAsync(callback.Id, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error in SupportCallbackHandler: {ex.Message}");
            await _botClient.AnswerCallbackQueryAsync(callback.Id, "⚠️ Ошибка", cancellationToken: ct);
        }
    }

    private async Task HandleSupportStartAsync(CallbackQuery callback, MyUser user, CancellationToken ct)
    {
        // Получаем последние заказы пользователя
        var orders = await _orderService.GetUserOrdersAsync(user.Id);
        var activeOrders = orders.Where(o => o.Status != OrderStatus.Cancelled &&
                                             o.Status != OrderStatus.Completed).ToList();

        if (!activeOrders.Any())
        {
            // Если нет активных заказов, предлагаем создать обращение без заказа
            await _userService.UpdateUserStateAsync(user.Id, BotState.WaitingForSupportMessage);

            await _botClient.EditMessageTextAsync(
                chatId: callback.Message!.Chat.Id,
                messageId: callback.Message.MessageId,
                text: "💬 **Чат с поддержкой**\n\n" +
                      "У вас нет активных заказов, но вы можете задать общий вопрос.\n" +
                      "Напишите ваше сообщение:",
                replyMarkup: new InlineKeyboardMarkup(
                    InlineKeyboardButton.WithCallbackData("🔙 Назад", "main_menu")),
                cancellationToken: ct);
        }
        else
        {
            // Показываем список заказов для выбора
            await _botClient.EditMessageTextAsync(
                chatId: callback.Message!.Chat.Id,
                messageId: callback.Message.MessageId,
                text: "📦 **Выберите заказ**\n\n" +
                      "По какому заказу у вас вопрос?",
                replyMarkup: InlineKeyboards.GetSupportOrderSelectionKeyboard(activeOrders),
                cancellationToken: ct);
        }
    }

    private async Task StartAdminReplyAsync(CallbackQuery callback, MyUser admin, int orderId, long clientId, bool isPhoto, CancellationToken ct)
    {
        if (!admin.IsAdmin)
        {
            await _botClient.AnswerCallbackQueryAsync(callback.Id, "🚫 Доступ запрещен", cancellationToken: ct);
            return;
        }

        // Сохраняем данные для ответа
        _adminStateService.SetEditingProductId(admin.Id, orderId);
        // Используем временное поле для хранения ID клиента (можно добавить отдельный метод в IAdminStateService)

        await _userService.UpdateUserStateAsync(admin.Id, BotState.AdminReplyingToSupport);

        if (isPhoto)
        {
            await _botClient.SendTextMessageAsync(
                chatId: callback.Message!.Chat.Id,
                text: $"📸 **Отправка фото в поддержку по заказу №{orderId}**\n\n" +
                      "Отправьте фото, которое хотите отправить клиенту:",
                cancellationToken: ct);
        }
        else
        {
            await _botClient.SendTextMessageAsync(
                chatId: callback.Message!.Chat.Id,
                text: $"💬 **Ответ поддержки по заказу №{orderId}**\n\n" +
                      "Напишите текст ответа клиенту:",
                cancellationToken: ct);
        }
    }

    private async Task ShowAdminSupportHistoryAsync(CallbackQuery callback, int orderId, CancellationToken ct)
    {
        var messages = await _supportService.GetOrderChatHistoryAsync(orderId);
        var order = await _orderService.GetOrderByIdAsync(orderId);

        if (order == null) return;

        if (!messages.Any())
        {
            await _botClient.EditMessageTextAsync(
                chatId: callback.Message!.Chat.Id,
                messageId: callback.Message.MessageId,
                text: $"💬 **История поддержки по заказу №{orderId}**\n\n" +
                      "Пока нет сообщений.",
                replyMarkup: new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🔙 К заказу", $"admin_order_view_{orderId}")
                    }
                }),
                cancellationToken: ct);
            return;
        }

        var chatHistory = $"💬 **История поддержки по заказу №{orderId}**\n\n";

        foreach (var msg in messages.OrderBy(m => m.SentAt))
        {
            var sender = msg.SenderType == SenderType.Client
                ? $"👤 Клиент (ID: {msg.SenderId})"
                : "👨‍💼 Админ";
            var time = msg.SentAt.ToString("dd.MM HH:mm");

            if (!string.IsNullOrEmpty(msg.PhotoFileId))
            {
                chatHistory += $"{sender} [{time}]: 📸 Фото\n";
            }
            else if (!string.IsNullOrEmpty(msg.MessageText))
            {
                chatHistory += $"{sender} [{time}]: {msg.MessageText}\n";
            }
        }

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📝 Ответить", $"admin_reply_support_{orderId}_{order.UserId}"),
                InlineKeyboardButton.WithCallbackData("📸 Отправить фото", $"admin_reply_photo_{orderId}_{order.UserId}")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🔙 К заказу", $"admin_order_view_{orderId}")
            }
        });

        await _botClient.EditMessageTextAsync(
            chatId: callback.Message!.Chat.Id,
            messageId: callback.Message.MessageId,
            text: chatHistory,
            replyMarkup: keyboard,
            cancellationToken: ct);
    }
}