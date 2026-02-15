using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using MyShopBotNET9.Models;
using MyShopBotNET9.Services;
using MyShopBotNET9.Handlers.Interfaces;
using MyShopBotNET9.Keyboards;
using MyUser = MyShopBotNET9.Models.User;

namespace MyShopBotNET9.Handlers.CallbackHandlers;

public class PaymentCallbackHandler : ICallbackHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly OrderService _orderService;
    private readonly UserService _userService;
    private readonly CartService _cartService;

    public PaymentCallbackHandler(
        ITelegramBotClient botClient,
        OrderService orderService,
        UserService userService,
        CartService cartService)
    {
        _botClient = botClient;
        _orderService = orderService;
        _userService = userService;
        _cartService = cartService;
    }

    // ⭐ ВАЖНО: Обрабатываем ОБЕ кнопки - и "paid" и "confirm_payment_"
    public bool CanHandle(string callbackData) =>
        callbackData == "paid" ||
        callbackData?.StartsWith("confirm_payment_") == true;

    public async Task HandleAsync(CallbackQuery callback, MyUser user, CancellationToken ct)
    {
        if (callback.Message == null || string.IsNullOrEmpty(callback.Data)) return;

        var data = callback.Data;

        // ⭐ ВАЖНО: Обработка СТАРОЙ кнопки "paid"
        if (data == "paid")
        {
            // Ищем последний заказ пользователя
            var userOrders = await _orderService.GetUserOrdersAsync(user.Id);
            var lastOrder = userOrders.OrderByDescending(o => o.OrderDate).FirstOrDefault();

            if (lastOrder != null)
            {
                await HandlePaymentConfirmationAsync(callback, user, lastOrder.Id, ct);
            }
            else
            {
                await _botClient.AnswerCallbackQueryAsync(
                    callback.Id,
                    "❌ Не найден активный заказ",
                    cancellationToken: ct);
            }
            return;
        }

        // ⭐ Обработка НОВОЙ кнопки "confirm_payment_" (из меню заказа)
        if (data.StartsWith("confirm_payment_"))
        {
            int orderId = int.Parse(data.Replace("confirm_payment_", ""));
            await HandlePaymentConfirmationAsync(callback, user, orderId, ct);
        }
    }

    // ⭐ ОБЩИЙ метод подтверждения оплаты
    private async Task HandlePaymentConfirmationAsync(CallbackQuery callback, MyUser user, int orderId, CancellationToken ct)
    {
        var order = await _orderService.GetOrderByIdAsync(orderId);

        if (order == null)
        {
            await _botClient.AnswerCallbackQueryAsync(
                callback.Id,
                "❌ Заказ не найден",
                cancellationToken: ct);
            return;
        }

        // Очищаем корзину
        await _cartService.ClearCartAsync(user.Id);

        // Возвращаем в главное меню
        await _userService.UpdateUserStateAsync(user.Id, BotState.MainMenu);

        // Обновляем сообщение для пользователя
        await _botClient.EditMessageTextAsync(
            chatId: callback.Message!.Chat.Id,
            messageId: callback.Message.MessageId,
            text: "🎉 **Заказ принят!**\n\nУведомление об оплате отправлено администратору. Ожидайте подтверждения статуса.",
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            replyMarkup: InlineKeyboards.GetMainMenuKeyboard(),
            cancellationToken: ct);

        // Уведомляем админов
        await NotifyAdminsAboutOrderAsync(order, user, ct);

        // Подтверждаем callback
        await _botClient.AnswerCallbackQueryAsync(callback.Id, "✅ Уведомление отправлено", cancellationToken: ct);
    }

    // ⭐ Улучшенное уведомление админов
    private async Task NotifyAdminsAboutOrderAsync(Order order, MyUser user, CancellationToken ct)
    {
        var adminUsers = await _userService.GetAdminsAsync();

        if (!adminUsers.Any()) return;

        var orderDetails = await GetOrderDetailsForAdminAsync(order, user);
        var adminKeyboard = GetAdminOrderControlKeyboard(order.Id);

        foreach (var admin in adminUsers)
        {
            try
            {
                await _botClient.SendTextMessageAsync(
                    chatId: admin.Id,
                    text: orderDetails,
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                    replyMarkup: adminKeyboard,
                    cancellationToken: ct);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Не удалось уведомить админа {admin.Id}: {ex.Message}");
            }
        }
    }

    private async Task<string> GetOrderDetailsForAdminAsync(Order order, MyUser user)
    {
        var orderWithItems = await _orderService.GetOrderByIdAsync(order.Id);

        var orderItemsText = "• Товары не найдены";
        if (orderWithItems?.OrderItems != null && orderWithItems.OrderItems.Any())
        {
            orderItemsText = string.Join("\n",
                orderWithItems.OrderItems.Select(i =>
                    $"• {i.ProductName} x{i.Quantity} по {i.Price}₽"));
        }

        var userCity = !string.IsNullOrEmpty(user.City) ? user.City : "Не указан";

        return $"💰 **НОВЫЙ ЗАКАЗ №{order.Id}**\n\n" +
               $"👤 **Клиент:** {user.FirstName ?? "Не указано"}\n" +
               $"🆔 **ID клиента:** `{user.Id}`\n" +
               $"🔗 **Username:** @{user.Username ?? "нет"}\n" +
               $"🏙️ **Город:** {userCity}\n\n" +
               $"📍 **Адрес доставки:** {order.Address ?? "Не указан"}\n" +
               $"💰 **Сумма:** {order.TotalAmount}₽\n" +
               $"📅 **Дата:** {order.OrderDate:dd.MM.yyyy HH:mm}\n\n" +
               $"🛒 **Товары:**\n{orderItemsText}\n\n" +
               $"📊 **Статус:** ⏳ Ожидает обработки";
    }

    private InlineKeyboardMarkup GetAdminOrderControlKeyboard(int orderId)
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("✅ Подтвердить оплату", $"admin_confirm_{orderId}"),
                InlineKeyboardButton.WithCallbackData("🚚 В доставку", $"admin_ship_{orderId}")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🎁 Доставлен", $"admin_deliver_{orderId}"),
                InlineKeyboardButton.WithCallbackData("📸 Фото", $"admin_send_photo_{orderId}")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📋 Детали заказа", $"admin_order_view_{orderId}"),
                InlineKeyboardButton.WithCallbackData("❌ Отменить", $"admin_cancel_{orderId}")
            }
        });
    }
}