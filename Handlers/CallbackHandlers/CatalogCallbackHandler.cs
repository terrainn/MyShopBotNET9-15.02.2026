using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using MyShopBotNET9.Models;
using MyShopBotNET9.Services;
using MyShopBotNET9.Handlers.Interfaces;
using MyShopBotNET9.Keyboards;
using Microsoft.Extensions.Logging;
using MyUser = MyShopBotNET9.Models.User;
using System.Globalization;

namespace MyShopBotNET9.Handlers.CallbackHandlers;

public class CatalogCallbackHandler : ICallbackHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly ICatalogService _catalogService;
    private readonly CartService _cartService;
    private readonly ILogger<CatalogCallbackHandler> _logger;

    // Временное хранилище последней выбранной категории для каждого пользователя
    private static Dictionary<long, string> _lastCategory = new();

    public CatalogCallbackHandler(ITelegramBotClient botClient,
        ICatalogService catalogService,
        CartService cartService,
        ILogger<CatalogCallbackHandler> logger)
    {
        _botClient = botClient;
        _catalogService = catalogService;
        _cartService = cartService;
        _logger = logger;
    }

    public bool CanHandle(string callbackData)
    {
        return callbackData?.StartsWith("category_") == true ||
               callbackData?.StartsWith("product_") == true ||
               callbackData?.StartsWith("select_gram_") == true ||
               callbackData?.StartsWith("add_to_cart_") == true ||
               callbackData == "back_to_products" ||
               callbackData == "back_to_categories" ||
               callbackData == "show_catalog";
    }

    public async Task HandleAsync(CallbackQuery callback, MyUser user, CancellationToken ct)
    {
        if (callback.Message == null) return;

        var data = callback.Data!;
        Console.WriteLine($"🎯 Catalog Callback: {data}");

        try
        {
            switch (data)
            {
                case "show_catalog":
                case "back_to_categories":
                    await ShowCategoriesAsync(callback, user, ct);
                    break;

                case "back_to_products":
                    if (_lastCategory.TryGetValue(user.Id, out string? lastCategory))
                    {
                        await ShowCategoryProductsAsync(callback, lastCategory, user, ct);
                    }
                    else
                    {
                        await ShowCategoriesAsync(callback, user, ct);
                    }
                    break;

                default:
                    if (data.StartsWith("category_"))
                    {
                        var categoryName = data.Replace("category_", "");
                        _lastCategory[user.Id] = categoryName; // Сохраняем категорию
                        await ShowCategoryProductsAsync(callback, categoryName, user, ct);
                    }
                    else if (data.StartsWith("product_"))
                    {
                        if (int.TryParse(data.Replace("product_", ""), out int productId))
                        {
                            await ShowGramSelectionAsync(callback, productId, ct);
                        }
                    }
                    else if (data.StartsWith("select_gram_"))
                    {
                        var parts = data.Split('_');
                        if (parts.Length == 4 &&
                            int.TryParse(parts[2], out int productId))
                        {
                            // Пробуем распарсить с разными культурами
                            string gramStr = parts[3];
                            decimal gram;

                            // Сначала пробуем как есть (с точкой)
                            if (!decimal.TryParse(gramStr, NumberStyles.Any, CultureInfo.InvariantCulture, out gram))
                            {
                                // Если не получилось, пробуем с запятой
                                decimal.TryParse(gramStr.Replace('.', ','), out gram);
                            }

                            if (gram > 0)
                            {
                                await ShowQuantitySelectionAsync(callback, productId, gram, ct);
                            }
                        }
                    }
                    else if (data.StartsWith("add_to_cart_"))
                    {
                        var parts = data.Split('_');
                        // add_to_cart_2_0.5_1
                        Console.WriteLine($"🔧 Parsing add_to_cart: {data}, parts count: {parts.Length}");

                        if (parts.Length >= 5)
                        {
                            if (int.TryParse(parts[3], out int productId))
                            {
                                // Последняя часть - количество
                                if (int.TryParse(parts[parts.Length - 1], out int quantity))
                                {
                                    // Все, что между productId и quantity - это граммовка
                                    string gramStr = string.Join("_", parts.Skip(4).Take(parts.Length - 5));
                                    if (string.IsNullOrEmpty(gramStr))
                                    {
                                        gramStr = parts[4];
                                    }

                                    Console.WriteLine($"🔧 Gram string: '{gramStr}'");

                                    // Парсим с инвариантной культурой (точка)
                                    if (decimal.TryParse(gramStr,
                                        System.Globalization.NumberStyles.Any,
                                        System.Globalization.CultureInfo.InvariantCulture,
                                        out decimal gram))
                                    {
                                        Console.WriteLine($"🔧 Parsed gram: {gram}");
                                        await AddToCartAsync(callback, user, productId, gram, quantity, ct);
                                    }
                                }
                            }
                        }
                    }
                    break;
            }

            await _botClient.AnswerCallbackQueryAsync(callback.Id, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("message is not modified"))
            {
                Console.WriteLine($"ℹ️ Сообщение не изменилось, пропускаем");
                await _botClient.AnswerCallbackQueryAsync(callback.Id, cancellationToken: ct);
            }
            else
            {
                Console.WriteLine($"❌ Error in CatalogCallbackHandler: {ex.Message}");
                await _botClient.AnswerCallbackQueryAsync(callback.Id, "❌ Произошла ошибка", cancellationToken: ct);
            }
        }
    }

    private async Task ShowCategoriesAsync(CallbackQuery callback, MyUser user, CancellationToken ct)
    {
        if (callback.Message == null) return;

        var categories = await _catalogService.GetCategoriesAsync(user.City);

        if (categories.Any())
        {
            await _botClient.EditMessageTextAsync(
                chatId: callback.Message.Chat.Id,
                messageId: callback.Message.MessageId,
                text: $"🏪 **Категории товаров - {user.City}**\n\nВыберите категорию:",
                replyMarkup: CatalogKeyboards.GetCategoriesKeyboard(categories),
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                cancellationToken: ct);
        }
        else
        {
            await _botClient.EditMessageTextAsync(
                chatId: callback.Message.Chat.Id,
                messageId: callback.Message.MessageId,
                text: $"📭 В вашем городе ({user.City}) пока нет товаров",
                cancellationToken: ct);
        }
    }

    private async Task ShowCategoryProductsAsync(CallbackQuery callback, string categoryName, MyUser user, CancellationToken ct)
    {
        if (callback.Message == null) return;

        var products = await _catalogService.GetProductsByCategoryAsync(categoryName, user.City);

        if (products.Any())
        {
            await _botClient.EditMessageTextAsync(
                chatId: callback.Message.Chat.Id,
                messageId: callback.Message.MessageId,
                text: $"📋 **Товары - {categoryName} ({user.City})**\n\nВыберите товар:",
                replyMarkup: CatalogKeyboards.GetProductsKeyboard(products),
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                cancellationToken: ct);
        }
        else
        {
            await _botClient.EditMessageTextAsync(
                chatId: callback.Message.Chat.Id,
                messageId: callback.Message.MessageId,
                text: $"📭 В категории '{categoryName}' для города {user.City} пока нет товаров",
                replyMarkup: CatalogKeyboards.GetBackToCategoriesKeyboard(),
                cancellationToken: ct);
        }
    }

    private async Task ShowGramSelectionAsync(CallbackQuery callback, int productId, CancellationToken ct)
    {
        if (callback.Message == null) return;

        var product = await _catalogService.GetProductByIdAsync(productId);
        if (product == null)
        {
            await _botClient.AnswerCallbackQueryAsync(callback.Id, "❌ Товар не найден", cancellationToken: ct);
            return;
        }

        var productText = $"🎁 **{product.Name}**\n\n" +
                         $"📝 {product.Description ?? "Описание отсутствует"}\n\n" +
                         $"Выберите граммовку:";

        var keyboard = CatalogKeyboards.GetGramSelectionKeyboard(product.Id, product.GramPrices);

        await _botClient.EditMessageTextAsync(
            chatId: callback.Message.Chat.Id,
            messageId: callback.Message.MessageId,
            text: productText,
            replyMarkup: keyboard,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            cancellationToken: ct);
    }

    private async Task ShowQuantitySelectionAsync(CallbackQuery callback, int productId, decimal gram, CancellationToken ct)
    {
        if (callback.Message == null) return;

        Console.WriteLine($"🔧 ShowQuantitySelectionAsync: productId={productId}, gram={gram}");

        var product = await _catalogService.GetProductByIdAsync(productId);
        if (product == null)
        {
            await _botClient.AnswerCallbackQueryAsync(callback.Id, "❌ Товар не найден", cancellationToken: ct);
            return;
        }

        if (!product.GramPrices.ContainsKey(gram))
        {
            Console.WriteLine($"❌ Gram {gram} not found in product prices");
            await _botClient.AnswerCallbackQueryAsync(callback.Id, "❌ Недоступная граммовка", cancellationToken: ct);
            return;
        }

        var price = product.GramPrices[gram];
        var text = $"🎁 **{product.Name}**\n" +
                  $"⚖️ {gram}г\n" +
                  $"💰 {price}₽ за шт.\n\n" +
                  $"Выберите количество:";

        var keyboard = CatalogKeyboards.GetQuantitySelectionKeyboard(productId, gram, price);

        await _botClient.EditMessageTextAsync(
            chatId: callback.Message.Chat.Id,
            messageId: callback.Message.MessageId,
            text: text,
            replyMarkup: keyboard,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            cancellationToken: ct);
    }

    private async Task AddToCartAsync(CallbackQuery callback, MyUser user, int productId, decimal gram, int quantity, CancellationToken ct)
    {
        try
        {
            var product = await _catalogService.GetProductByIdAsync(productId);
            if (product == null)
            {
                await _botClient.AnswerCallbackQueryAsync(callback.Id, "❌ Товар не найден", cancellationToken: ct);
                return;
            }

            if (!product.GramPrices.ContainsKey(gram))
            {
                await _botClient.AnswerCallbackQueryAsync(callback.Id, "❌ Недоступная граммовка", cancellationToken: ct);
                return;
            }

            if (product.StockQuantity < quantity)
            {
                await _botClient.AnswerCallbackQueryAsync(callback.Id, $"❌ В наличии только {product.StockQuantity} шт.", cancellationToken: ct);
                return;
            }

            await _cartService.AddToCartAsync(user.Id, productId, quantity, gram);

            product.StockQuantity -= quantity;
            await _catalogService.UpdateProductAsync(product);

            await _botClient.AnswerCallbackQueryAsync(
                callback.Id,
                $"✅ {quantity} шт. по {gram}г добавлено в корзину",
                cancellationToken: ct);

            // Возвращаемся к выбору граммовок этого же товара
            await ShowGramSelectionAsync(callback, productId, ct);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error adding to cart: {ex.Message}");
            await _botClient.AnswerCallbackQueryAsync(callback.Id, "❌ Ошибка добавления", cancellationToken: ct);
        }
    }
}