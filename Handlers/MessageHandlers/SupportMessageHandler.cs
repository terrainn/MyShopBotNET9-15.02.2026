using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using MyShopBotNET9.Models;
using MyShopBotNET9.Services;
using MyShopBotNET9.Handlers.Interfaces;
using MyShopBotNET9.Keyboards;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyUser = MyShopBotNET9.Models.User;

namespace MyShopBotNET9.Handlers.MessageHandlers;

public class SupportMessageHandler : IMessageHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly SupportService _supportService;
    private readonly UserService _userService;
    private readonly OrderService _orderService;
    private readonly IAdminStateService _adminStateService;
    private readonly IServiceProvider _serviceProvider;

    public SupportMessageHandler(
        ITelegramBotClient botClient,
        SupportService supportService,
        UserService userService,
        OrderService orderService,
        IAdminStateService adminStateService,
        IServiceProvider serviceProvider)
    {
        _botClient = botClient;
        _supportService = supportService;
        _userService = userService;
        _orderService = orderService;
        _adminStateService = adminStateService;
        _serviceProvider = serviceProvider;
    }

    public bool CanHandle(string text, BotState state)
    {
        return state == BotState.WaitingForSupportMessage ||
               state == BotState.AdminReplyingToSupport;
    }

    public async Task HandleAsync(Message message, MyUser user, CancellationToken ct)
    {
        if (user.CurrentState == BotState.WaitingForSupportMessage)
        {
            await HandleClientMessageAsync(message, user, ct);
        }
        else if (user.CurrentState == BotState.AdminReplyingToSupport)
        {
            await HandleAdminReplyAsync(message, user, ct);
        }
    }

    private async Task HandleClientMessageAsync(Message message, MyUser user, CancellationToken ct)
    {
        // Получаем ID заказа из временного хранилища
        int? orderId = _adminStateService.GetEditingProductId(user.Id);

        if (orderId == null)
        {
            // Если нет конкретного заказа, создаем обращение без заказа
            await CreateGeneralSupportTicketAsync(message, user, ct);
            return;
        }

        // Проверяем, что заказ принадлежит пользователю
        var order = await _orderService.GetOrderByIdAsync(orderId.Value);
        if (order == null || order.UserId != user.Id)
        {
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "❌ Ошибка: заказ не найден",
                cancellationToken: ct);

            _adminStateService.ClearEditingState(user.Id);
            await _userService.UpdateUserStateAsync(user.Id, BotState.MainMenu);
            return;
        }

        SupportMessage supportMessage;

        // Обрабатываем фото
        if (message.Photo != null && message.Photo.Length > 0)
        {
            var photo = message.Photo.Last();
            supportMessage = await _supportService.SaveClientMessageAsync(
                orderId.Value,
                user.Id,
                message.Caption, // Текст может быть в подписи к фото
                photo.FileId);

            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "✅ Фото отправлено в поддержку!",
                cancellationToken: ct);
        }
        // Обрабатываем текст
        else if (!string.IsNullOrEmpty(message.Text))
        {
            supportMessage = await _supportService.SaveClientMessageAsync(
                orderId.Value,
                user.Id,
                message.Text,
                null);

            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "✅ Сообщение отправлено в поддержку!",
                cancellationToken: ct);
        }
        else
        {
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "❌ Пожалуйста, отправьте текст или фото",
                cancellationToken: ct);
            return;
        }

        // Уведомляем всех админов
        await NotifyAdminsAboutNewMessageAsync(order, user, supportMessage, ct);

        // Возвращаем пользователя в меню заказа
        await ShowOrderDetailsAfterMessageAsync(message.Chat.Id, orderId.Value, user.Id, ct);

        // Очищаем состояние
        _adminStateService.ClearEditingState(user.Id);
        await _userService.UpdateUserStateAsync(user.Id, BotState.MainMenu);
    }

    private async Task HandleAdminReplyAsync(Message message, MyUser admin, CancellationToken ct)
    {
        if (!admin.IsAdmin)
        {
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "🚫 Доступ запрещен",
                cancellationToken: ct);
            return;
        }

        // Получаем данные из временного хранилища
        int? orderId = _adminStateService.GetEditingProductId(admin.Id);

        if (orderId == null)
        {
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "❌ Ошибка: не найден заказ для ответа",
                cancellationToken: ct);

            await _userService.UpdateUserStateAsync(admin.Id, BotState.AdminPanel);
            return;
        }

        var order = await _orderService.GetOrderByIdAsync(orderId.Value);
        if (order == null)
        {
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "❌ Заказ не найден",
                cancellationToken: ct);
            return;
        }

        SupportMessage supportMessage;

        // Обрабатываем фото от админа
        if (message.Photo != null && message.Photo.Length > 0)
        {
            var photo = message.Photo.Last();
            supportMessage = await _supportService.SaveAdminMessageAsync(
                orderId.Value,
                admin.Id,
                message.Caption,
                photo.FileId);

            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "✅ Фото отправлено клиенту!",
                cancellationToken: ct);
        }
        // Обрабатываем текст от админа
        else if (!string.IsNullOrEmpty(message.Text))
        {
            supportMessage = await _supportService.SaveAdminMessageAsync(
                orderId.Value,
                admin.Id,
                message.Text,
                null);

            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "✅ Ответ отправлен клиенту!",
                cancellationToken: ct);
        }
        else
        {
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "❌ Пожалуйста, отправьте текст или фото",
                cancellationToken: ct);
            return;
        }

        // Отправляем сообщение клиенту
        await SendMessageToClientAsync(order.UserId, order.Id, supportMessage, ct);

        // Показываем админу обновленную историю
        await ShowAdminOrderAfterReplyAsync(message.Chat.Id, order.Id, ct);

        // Очищаем состояние
        _adminStateService.ClearEditingState(admin.Id);
        await _userService.UpdateUserStateAsync(admin.Id, BotState.AdminPanel);
    }

    private async Task CreateGeneralSupportTicketAsync(Message message, MyUser user, CancellationToken ct)
    {
        // Создаем "виртуальный" заказ с ID = 0 для общих вопросов
        const int generalOrderId = 0;

        if (!string.IsNullOrEmpty(message.Text))
        {
            await _supportService.SaveClientMessageAsync(generalOrderId, user.Id, message.Text, null);

            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "✅ Ваш общий вопрос отправлен в поддержку!",
                replyMarkup: InlineKeyboards.GetMainMenuKeyboard(),
                cancellationToken: ct);
        }
        else if (message.Photo != null && message.Photo.Length > 0)
        {
            var photo = message.Photo.Last();
            await _supportService.SaveClientMessageAsync(generalOrderId, user.Id, message.Caption, photo.FileId);

            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "✅ Фото отправлено в поддержку!",
                replyMarkup: InlineKeyboards.GetMainMenuKeyboard(),
                cancellationToken: ct);
        }

        await _userService.UpdateUserStateAsync(user.Id, BotState.MainMenu);
    }

    private async Task NotifyAdminsAboutNewMessageAsync(Order order, MyUser client, SupportMessage message, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<UserService>();
        var admins = await userService.GetAdminsAsync();

        var messageType = !string.IsNullOrEmpty(message.PhotoFileId) ? "📸 Фото" : "💬 Сообщение";

        foreach (var admin in admins)
        {
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        "📝 Ответить текстом",
                        $"admin_reply_support_{order.Id}_{client.Id}")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        "📸 Ответить фото",
                        $"admin_reply_photo_{order.Id}_{client.Id}")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        "📋 История чата",
                        $"admin_support_history_{order.Id}")
                }
            });

            var clientName = !string.IsNullOrEmpty(client.FirstName)
                ? client.FirstName
                : $"Клиент {client.Id}";

            var notificationText = $"📩 **Новое обращение по заказу №{order.Id}**\n\n" +
                                  $"👤 От: {clientName}\n" +
                                  $"🆔 ID клиента: `{client.Id}`\n" +
                                  $"💬 Тип: {messageType}\n" +
                                  $"⏰ Время: {message.SentAt:HH:mm}\n\n";

            if (!string.IsNullOrEmpty(message.MessageText))
            {
                notificationText += $"📝 Текст: {message.MessageText}";
            }

            await _botClient.SendTextMessageAsync(
                chatId: admin.Id,
                text: notificationText,
                parseMode: ParseMode.Markdown,
                replyMarkup: keyboard,
                cancellationToken: ct);
        }
    }

    private async Task SendMessageToClientAsync(long clientId, int orderId, SupportMessage message, CancellationToken ct)
    {
        try
        {
            if (!string.IsNullOrEmpty(message.PhotoFileId))
            {
                var caption = $"📩 **Ответ от поддержки по заказу №{orderId}**\n\n";
                if (!string.IsNullOrEmpty(message.MessageText))
                {
                    caption += $"💬 {message.MessageText}";
                }

                await _botClient.SendPhotoAsync(
                    chatId: clientId,
                    photo: InputFile.FromFileId(message.PhotoFileId),
                    caption: caption,
                    parseMode: ParseMode.Markdown,
                    cancellationToken: ct);
            }
            else if (!string.IsNullOrEmpty(message.MessageText))
            {
                var text = $"📩 **Ответ от поддержки по заказу №{orderId}**\n\n" +
                          $"💬 {message.MessageText}";

                await _botClient.SendTextMessageAsync(
                    chatId: clientId,
                    text: text,
                    parseMode: ParseMode.Markdown,
                    replyMarkup: new InlineKeyboardMarkup(
                        InlineKeyboardButton.WithCallbackData(
                            "📦 Перейти к заказу",
                            $"order_details_{orderId}")),
                    cancellationToken: ct);
            }

            // Отмечаем сообщение как доставленное
            await _supportService.MarkMessagesAsReadByClientAsync(orderId, clientId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Ошибка отправки сообщения клиенту {clientId}: {ex.Message}");
        }
    }

    private async Task ShowOrderDetailsAfterMessageAsync(long chatId, int orderId, long userId, CancellationToken ct)
    {
        var order = await _orderService.GetOrderByIdAsync(orderId);
        if (order == null) return;

        var details = $"🆔 **Заказ №{order.Id}**\n" +
                     $"──────────────────\n" +
                     $"💰 **Сумма:** {order.TotalAmount}₽\n" +
                     $"📍 **Адрес:** {order.Address ?? "Не указан"}\n" +
                     $"📊 **Статус:** {GetOrderStatusEmoji(order.Status)} {order.Status}\n\n" +
                     $"✅ Ваше сообщение отправлено в поддержку. Мы ответим как можно скорее!";

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("💬 Написать еще", $"support_order_{orderId}"),
                InlineKeyboardButton.WithCallbackData("📋 История чата", $"support_history_{orderId}")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🔙 К заказам", "show_orders")
            }
        });

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: details,
            parseMode: ParseMode.Markdown,
            replyMarkup: keyboard,
            cancellationToken: ct);
    }

    private async Task ShowAdminOrderAfterReplyAsync(long chatId, int orderId, CancellationToken ct)
    {
        var order = await _orderService.GetOrderByIdAsync(orderId);
        if (order == null) return;

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📋 История чата", $"admin_support_history_{orderId}"),
                InlineKeyboardButton.WithCallbackData("🔙 К заказу", $"admin_order_view_{orderId}")
            }
        });

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: $"✅ Ответ отправлен клиенту по заказу №{orderId}",
            replyMarkup: keyboard,
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