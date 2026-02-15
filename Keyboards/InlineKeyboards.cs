using Telegram.Bot.Types.ReplyMarkups;
using MyShopBotNET9.Models;

namespace MyShopBotNET9.Keyboards;

public static class InlineKeyboards
{
    // Главное меню для обычных пользователей
    public static InlineKeyboardMarkup GetMainMenuKeyboard()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📋 Каталог", "show_catalog"),
                InlineKeyboardButton.WithCallbackData("🛒 Корзина", "show_cart")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📦 Мои заказы", "my_orders"),
                InlineKeyboardButton.WithCallbackData("👤 Профиль", "show_profile")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("💬 Поддержка", "support_start")
            }
        });
    }

    // Главное меню для админов
    public static InlineKeyboardMarkup GetAdminMainMenuKeyboard()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📋 Каталог", "show_catalog"),
                InlineKeyboardButton.WithCallbackData("🛒 Корзина", "show_cart")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📦 Мои заказы", "my_orders"),
                InlineKeyboardButton.WithCallbackData("👤 Профиль", "show_profile")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("⚙️ Админка", "show_admin"),
                InlineKeyboardButton.WithCallbackData("💬 Поддержка", "support_start")
            }
        });
    }

    // Меню выбора заказа для обращения в поддержку
    public static InlineKeyboardMarkup GetSupportOrderSelectionKeyboard(List<Order> orders)
    {
        var buttons = new List<InlineKeyboardButton[]>();

        foreach (var order in orders.Take(5)) // Показываем последние 5 заказов
        {
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    $"📦 Заказ №{order.Id} от {order.OrderDate:dd.MM} - {order.TotalAmount}₽",
                    $"support_order_{order.Id}")
            });
        }

        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData("🔙 Назад в меню", "main_menu")
        });

        return new InlineKeyboardMarkup(buttons);
    }

    // Клавиатура в чате поддержки (для клиента)
    public static InlineKeyboardMarkup GetSupportChatKeyboard(int orderId)
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📋 История переписки", $"support_history_{orderId}"),
                InlineKeyboardButton.WithCallbackData("🔙 К заказу", $"order_details_{orderId}")
            }
        });
    }

    // Клавиатура для админа в чате поддержки
    public static InlineKeyboardMarkup GetAdminSupportChatKeyboard(int orderId, long clientId)
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📝 Написать ответ", $"admin_reply_support_{orderId}_{clientId}"),
                InlineKeyboardButton.WithCallbackData("📸 Отправить фото", $"admin_reply_photo_{orderId}_{clientId}")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📋 История", $"admin_support_history_{orderId}"),
                InlineKeyboardButton.WithCallbackData("🔙 К заказу", $"admin_order_view_{orderId}")
            }
        });
    }

    // Клавиатура для деталей заказа (с индикатором новых сообщений)
    public static InlineKeyboardMarkup GetOrderDetailsKeyboard(int orderId, OrderStatus status, bool hasUnreadMessages = false)
    {
        var buttons = new List<InlineKeyboardButton[]>();

        if (status == OrderStatus.Pending)
        {
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData("✅ Подтвердить оплату", "paid"),
                InlineKeyboardButton.WithCallbackData("❌ Отменить заказ", $"cancel_order_{orderId}")
            });
        }

        // Кнопка поддержки с индикатором новых сообщений
        var supportButtonText = hasUnreadMessages
            ? "💬 Поддержка (🔴 новое)"
            : "💬 Поддержка";

        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData(supportButtonText, $"support_order_{orderId}")
        });

        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData("📦 К списку заказов", "show_orders"),
            InlineKeyboardButton.WithCallbackData("🔙 Главное меню", "main_menu")
        });

        return new InlineKeyboardMarkup(buttons);
    }

    // Остальные существующие методы (GetCartKeyboard, GetEmptyCartKeyboard и т.д.) оставляем без изменений
    public static InlineKeyboardMarkup GetCartKeyboard(bool hasItems = true)
    {
        var buttons = new List<InlineKeyboardButton[]>();

        if (hasItems)
        {
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData("💳 Оформить заказ", "checkout"),
                InlineKeyboardButton.WithCallbackData("🗑️ Очистить корзину", "clear_cart")
            });
        }

        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData("📋 Продолжить покупки", "show_catalog"),
            InlineKeyboardButton.WithCallbackData("🔙 Главное меню", "main_menu")
        });

        return new InlineKeyboardMarkup(buttons);
    }

    public static InlineKeyboardMarkup GetEmptyCartKeyboard()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📋 Перейти к каталогу", "show_catalog")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🔙 Главное меню", "main_menu")
            }
        });
    }

    public static InlineKeyboardMarkup GetEmptyOrdersKeyboard()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🛒 Перейти к покупкам", "show_catalog")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🔙 Главное меню", "main_menu")
            }
        });
    }

    public static InlineKeyboardMarkup GetCheckoutKeyboard()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("✅ Создать заказ", "create_order")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🔙 Назад к корзине", "show_cart")
            }
        });
    }

    public static InlineKeyboardMarkup GetPaymentKeyboard(int orderId)
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("✅ Оплатил", "paid"),
                InlineKeyboardButton.WithCallbackData("❌ Отменить", $"payment_cancel_{orderId}")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🔙 К заказам", "show_orders")
            }
        });
    }
}