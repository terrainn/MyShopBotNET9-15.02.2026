using Telegram.Bot.Types.ReplyMarkups;
using MyShopBotNET9.Models;
using System.Collections.Generic;

namespace MyShopBotNET9.Keyboards;

public static class AdminKeyboards
{
    public static InlineKeyboardMarkup GetAdminMainKeyboard()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📊 Статистика", "admin_stats"),
                InlineKeyboardButton.WithCallbackData("📦 Заказы", "admin_orders")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🎁 Товары", "admin_products"),
                InlineKeyboardButton.WithCallbackData("👥 Пользователи", "admin_users")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📬 Поддержка", "admin_support_requests")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🔙 Главное меню", "main_menu")
            }
        });
    }

    public static InlineKeyboardMarkup GetProductManagementKeyboard()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📥 Добавить товар", "admin_add_product"),
                InlineKeyboardButton.WithCallbackData("✏️ Редактировать", "admin_edit_product")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🔙 Назад", "show_admin"),
                InlineKeyboardButton.WithCallbackData("🔙 Главное меню", "main_menu")
            }
        });
    }

    public static InlineKeyboardMarkup GetAdminProductListKeyboard(IEnumerable<Product> products)
    {
        var buttons = new List<InlineKeyboardButton[]>();

        foreach (var product in products)
        {
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    $"{(product.IsActive ? "✅" : "❌")} {product.Name} ({product.City})",
                    $"edit_product_{product.Id}")
            });
        }

        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Назад", "admin_products") });
        return new InlineKeyboardMarkup(buttons);
    }

    public static InlineKeyboardMarkup GetCancelOperationKeyboard()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("❌ Отменить операцию", "admin_cancel") }
        });
    }

    public static InlineKeyboardMarkup GetBackToProductsKeyboard()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("🔙 К списку товаров", "admin_edit_product") }
        });
    }
}