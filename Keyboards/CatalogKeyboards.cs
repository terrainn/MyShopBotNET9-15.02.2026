using Telegram.Bot.Types.ReplyMarkups;
using MyShopBotNET9.Models;

namespace MyShopBotNET9.Keyboards;

public static class CatalogKeyboards
{
    // Доступные граммовки
    private static readonly decimal[] AvailableGrams = { 0.5m, 1.0m, 2.0m, 3.0m, 4.0m, 5.0m, 10.0m };

    public static InlineKeyboardMarkup GetCategoriesKeyboard(List<string> categories)
    {
        var buttons = new List<InlineKeyboardButton[]>();

        for (int i = 0; i < categories.Count; i += 2)
        {
            var row = new List<InlineKeyboardButton>();
            row.Add(InlineKeyboardButton.WithCallbackData(
                categories[i],
                $"category_{categories[i]}"));

            if (i + 1 < categories.Count)
            {
                row.Add(InlineKeyboardButton.WithCallbackData(
                    categories[i + 1],
                    $"category_{categories[i + 1]}"));
            }

            buttons.Add(row.ToArray());
        }

        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData("🛒 Корзина", "show_cart"),
            InlineKeyboardButton.WithCallbackData("🔙 Главное меню", "main_menu")
        });

        return new InlineKeyboardMarkup(buttons);
    }

    public static InlineKeyboardMarkup GetProductsKeyboard(List<Product> products)
    {
        var buttons = new List<InlineKeyboardButton[]>();

        foreach (var product in products)
        {
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    $"🎁 {product.Name} - от {product.GramPrices.Values.Min()}₽",
                    $"product_{product.Id}")
            });
        }

        if (buttons.Count == 0)
        {
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData("📭 Товары отсутствуют", "back_to_categories")
            });
        }

        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData("🔙 Назад к категориям", "back_to_categories"),
            InlineKeyboardButton.WithCallbackData("🔙 Главное меню", "main_menu")
        });

        return new InlineKeyboardMarkup(buttons);
    }

    public static InlineKeyboardMarkup GetGramSelectionKeyboard(int productId, Dictionary<decimal, decimal> gramPrices)
    {
        var buttons = new List<InlineKeyboardButton[]>();
        var row = new List<InlineKeyboardButton>();

        foreach (var gram in gramPrices.Keys.OrderBy(g => g))
        {
            // Принудительно используем точку как разделитель
            string gramStr = gram.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string callbackData = $"select_gram_{productId}_{gramStr}";

            Console.WriteLine($"🔧 Граммовка: {gram} -> {callbackData}");

            row.Add(InlineKeyboardButton.WithCallbackData(
                $"{gram}г - {gramPrices[gram]}₽",
                callbackData));

            if (row.Count == 2)
            {
                buttons.Add(row.ToArray());
                row = new List<InlineKeyboardButton>();
            }
        }

        if (row.Count > 0)
            buttons.Add(row.ToArray());

        buttons.Add(new[]
        {
        InlineKeyboardButton.WithCallbackData("🔙 Назад к товарам", "back_to_products")
    });

        return new InlineKeyboardMarkup(buttons);
    }

    public static InlineKeyboardMarkup GetQuantitySelectionKeyboard(int productId, decimal selectedGram, decimal price)
    {
        // Принудительно используем точку как разделитель
        string gramStr = selectedGram.ToString(System.Globalization.CultureInfo.InvariantCulture);

        Console.WriteLine($"🔧 Creating quantity keyboard: product={productId}, gram={selectedGram} -> callback gramStr={gramStr}");

        return new InlineKeyboardMarkup(new[]
        {
        new[] { InlineKeyboardButton.WithCallbackData("1 шт", $"add_to_cart_{productId}_{gramStr}_1") },
        new[] { InlineKeyboardButton.WithCallbackData("2 шт", $"add_to_cart_{productId}_{gramStr}_2") },
        new[] { InlineKeyboardButton.WithCallbackData("3 шт", $"add_to_cart_{productId}_{gramStr}_3") },
        new[] { InlineKeyboardButton.WithCallbackData("5 шт", $"add_to_cart_{productId}_{gramStr}_5") },
        new[] { InlineKeyboardButton.WithCallbackData("🔙 Назад к граммовкам", "back_to_products") }
    });
    }

    public static InlineKeyboardMarkup GetBackToCategoriesKeyboard()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🔙 Назад к категориям", "back_to_categories"),
                InlineKeyboardButton.WithCallbackData("🔙 Главное меню", "main_menu")
            }
        });
    }
}