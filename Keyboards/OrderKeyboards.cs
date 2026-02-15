using Telegram.Bot.Types.ReplyMarkups;
using MyShopBotNET9.Models;

namespace MyShopBotNET9.Keyboards;

public static class OrderKeyboards
{
    public static InlineKeyboardMarkup GetOrderActionsKeyboard(int orderId)
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("✅ Подтвердить оплату", $"confirm_payment_{orderId}"),
                InlineKeyboardButton.WithCallbackData("❌ Отменить заказ", $"cancel_order_{orderId}")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🔙 Назад к заказам", "back_to_orders"),
                InlineKeyboardButton.WithCallbackData("🔙 Главное меню", "main_menu")
            }
        });
    }

    public static InlineKeyboardMarkup GetOrdersListKeyboard(List<Order> orders)
    {
        var buttons = new List<InlineKeyboardButton[]>();

        foreach (var order in orders.Take(10)) // Ограничим для читаемости
        {
            var statusEmoji = order.Status switch
            {
                OrderStatus.Pending => "⏳",
                OrderStatus.Confirmed => "✅",
                OrderStatus.Shipped => "🚚",
                OrderStatus.Delivered => "📦",
                OrderStatus.Completed => "🎉",
                OrderStatus.Cancelled => "❌",
                _ => "📝"
            };

            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    $"{statusEmoji} Заказ #{order.Id} - {order.TotalAmount}₽",
                    $"order_details_{order.Id}")
            });
        }

        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData("🔙 Главное меню", "main_menu")
        });

        return new InlineKeyboardMarkup(buttons);
    }

    public static InlineKeyboardMarkup GetBackToOrdersKeyboard()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🔙 Назад к заказам", "back_to_orders"),
                InlineKeyboardButton.WithCallbackData("🔙 Главное меню", "main_menu")
            }
        });
    }

    // УДАЛИТЕ ВСЕ ДОПОЛНИТЕЛЬНЫЕ МЕТОДЫ ПОСЛЕ ЭТОГО
}