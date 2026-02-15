using Telegram.Bot;
using Telegram.Bot.Types;
using MyShopBotNET9.Models;
using MyShopBotNET9.Services;
using MyShopBotNET9.Data;
using MyShopBotNET9.Handlers.Interfaces;
using Microsoft.EntityFrameworkCore;
using MyUser = MyShopBotNET9.Models.User;

namespace MyShopBotNET9.Handlers.MessageHandlers;

public class AdminProductMessageHandler : IMessageHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly AppDbContext _context;
    private readonly IAdminStateService _adminStateService;
    private readonly UserService _userService;
    private readonly AdminService _adminService;

    public AdminProductMessageHandler(
        ITelegramBotClient botClient,
        AppDbContext context,
        IAdminStateService adminStateService,
        UserService userService,
        AdminService adminService)
    {
        _botClient = botClient;
        _context = context;
        _adminStateService = adminStateService;
        _userService = userService;
        _adminService = adminService;
    }

    public bool CanHandle(string text, BotState state) =>
        state == BotState.AdminWaitingForProductName ||
        state == BotState.AdminWaitingForProductPrice ||
        state == BotState.AdminWaitingForProductDescription ||
        state == BotState.AdminWaitingForProductCategory ||
        state == BotState.AdminWaitingForProductCity ||
        state == BotState.AdminWaitingForProductStock ||
        state == BotState.AdminWaitingForProductGramPrices;

    public async Task HandleAsync(Message message, MyUser user, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(message.Text)) return;

        Console.WriteLine($"🔄 AdminProductMessageHandler: state={user.CurrentState}, text='{message.Text}'");

        var adminState = _adminStateService.GetProductState(user.Id);
        int? editingProductId = _adminStateService.GetEditingProductId(user.Id);

        if (editingProductId != null)
        {
            await HandleEditExistingProductAsync(message, user, editingProductId.Value, ct);
        }
        else
        {
            await HandleCreateNewProductAsync(message, user, adminState, ct);
        }
    }

    private async Task HandleCreateNewProductAsync(Message message, MyUser user, AdminAddProductState? adminState, CancellationToken ct)
    {
        if (adminState == null)
        {
            _adminStateService.StartProductCreation(user.Id);
            adminState = _adminStateService.GetProductState(user.Id);
        }

        if (adminState == null)
        {
            await _botClient.SendTextMessageAsync(
                message.Chat.Id,
                "❌ Ошибка создания состояния товара. Попробуйте еще раз.",
                cancellationToken: ct);
            return;
        }

        bool success = false;
        string nextQuestion = "";

        switch (user.CurrentState)
        {
            case BotState.AdminWaitingForProductName:
                _adminStateService.SaveProductName(user.Id, message.Text ?? "");
                nextQuestion = "💰 Введите цену за 1 грамм (только число, например: 1500):";
                success = true;
                break;

            case BotState.AdminWaitingForProductPrice:
                if (decimal.TryParse(message.Text?.Replace(" ", "") ?? "", out decimal price))
                {
                    _adminStateService.SaveProductPrice(user.Id, price);
                    nextQuestion = "📝 Введите описание товара:";
                    success = true;
                }
                break;

            case BotState.AdminWaitingForProductDescription:
                _adminStateService.SaveProductDescription(user.Id, message.Text ?? "");
                nextQuestion = "📦 Введите количество товара на складе (только число):";
                success = true;
                break;

            case BotState.AdminWaitingForProductStock:
                if (int.TryParse(message.Text, out int stock))
                {
                    _adminStateService.SaveProductStock(user.Id, stock);
                    nextQuestion = "🗂️ Введите категорию товара:";
                    success = true;
                }
                break;

            case BotState.AdminWaitingForProductCategory:
                _adminStateService.SaveProductCategory(user.Id, message.Text ?? "");
                nextQuestion = "🏙️ Введите город для товара (или 'Все' для всех городов):";
                success = true;
                break;

            case BotState.AdminWaitingForProductCity:
                _adminStateService.SaveProductCity(user.Id, message.Text ?? "");
                nextQuestion = "⚖️ Введите цены для разных граммовок в формате:\n" +
                              "грамм1:цена1, грамм2:цена2\n\n" +
                              "Пример: 0.5:800, 1:1500, 2:2800, 3:4000, 5:6500\n\n" +
                              "Если цена такая же как за 1г, можно не указывать";
                success = true;
                break;

            case BotState.AdminWaitingForProductGramPrices:
                var parsedPrices = ParseGramPrices(message.Text ?? "");
                if (parsedPrices.Count > 0)
                {
                    adminState.GramPrices = parsedPrices;

                    try
                    {
                        var product = new Product
                        {
                            Name = adminState.ProductName!,
                            Price = adminState.Price ?? (parsedPrices.ContainsKey(1.0m) ? parsedPrices[1.0m] : parsedPrices.Values.First()),
                            Description = adminState.Description,
                            StockQuantity = adminState.StockQuantity ?? 0,
                            Category = adminState.Category,
                            City = adminState.City,
                            ImageUrl = adminState.ImageUrl ?? "https://via.placeholder.com/300",
                            IsActive = true
                        };

                        product.GramPrices = parsedPrices; // ← ЭТО САМОЕ ВАЖНОЕ

                        await _adminService.AddProductAsync(product);
                        _adminStateService.ClearProductState(user.Id);
                        await _userService.UpdateUserStateAsync(user.Id, BotState.AdminPanel);

                        var pricesText = string.Join("\n", parsedPrices.Select(p => $"• {p.Key}г - {p.Value}₽"));

                        await _botClient.SendTextMessageAsync(
                            message.Chat.Id,
                            $"✅ Товар '{product.Name}' успешно создан!\n\n" +
                            $"💰 Цены:\n{pricesText}\n\n" +
                            $"📦 Количество: {product.StockQuantity} шт.\n" +
                            $"🗂️ Категория: {product.Category}\n" +
                            $"🏙️ Город: {product.City}",
                            replyMarkup: MyShopBotNET9.Keyboards.AdminKeyboards.GetProductManagementKeyboard(),
                            cancellationToken: ct);

                        return;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error creating product: {ex.Message}");
                        await _botClient.SendTextMessageAsync(
                            message.Chat.Id,
                            $"❌ Ошибка создания товара: {ex.Message}",
                            cancellationToken: ct);
                        return;
                    }
                }
                else
                {
                    await _botClient.SendTextMessageAsync(
                        message.Chat.Id,
                        "❌ Неверный формат. Попробуйте еще раз:\n\nПример: 0.5:800, 1:1500, 2:2800",
                        cancellationToken: ct);
                    return;
                }
        }

        if (success)
        {
            BotState nextState = user.CurrentState switch
            {
                BotState.AdminWaitingForProductName => BotState.AdminWaitingForProductPrice,
                BotState.AdminWaitingForProductPrice => BotState.AdminWaitingForProductDescription,
                BotState.AdminWaitingForProductDescription => BotState.AdminWaitingForProductStock,
                BotState.AdminWaitingForProductStock => BotState.AdminWaitingForProductCategory,
                BotState.AdminWaitingForProductCategory => BotState.AdminWaitingForProductCity,
                BotState.AdminWaitingForProductCity => BotState.AdminWaitingForProductGramPrices,
                _ => BotState.AdminPanel
            };

            await _userService.UpdateUserStateAsync(user.Id, nextState);
            await _botClient.SendTextMessageAsync(message.Chat.Id, nextQuestion, cancellationToken: ct);
        }
        else
        {
            await _botClient.SendTextMessageAsync(
                message.Chat.Id,
                "❌ Неверный формат. Попробуйте еще раз:",
                cancellationToken: ct);
        }
    }

    private Dictionary<decimal, decimal> ParseGramPrices(string input)
    {
        var result = new Dictionary<decimal, decimal>();
        var parts = input.Split(',', StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            var pair = part.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (pair.Length == 2)
            {
                if (decimal.TryParse(pair[0].Trim().Replace('.', ','), out decimal gram) &&
                    decimal.TryParse(pair[1].Trim().Replace(' ', '0'), out decimal price))
                {
                    result[gram] = price;
                }
            }
        }

        return result;
    }

    private async Task HandleEditExistingProductAsync(Message message, MyUser user, int productId, CancellationToken ct)
    {
        var product = await _context.Products.FindAsync(new object?[] { productId }, ct);
        if (product == null)
        {
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "❌ Товар не найден",
                cancellationToken: ct);
            await _userService.UpdateUserStateAsync(user.Id, BotState.AdminPanel);
            return;
        }

        bool success = false;
        string fieldName = "";

        switch (user.CurrentState)
        {
            case BotState.AdminWaitingForProductName:
                product.Name = message.Text ?? "";
                fieldName = "название";
                success = true;
                break;
            case BotState.AdminWaitingForProductPrice:
                if (decimal.TryParse(message.Text?.Replace(" ", "") ?? "", out decimal price))
                {
                    var prices = product.GramPrices;
                    if (prices.ContainsKey(1.0m))
                        prices[1.0m] = price;
                    product.GramPrices = prices;
                    fieldName = "цена за 1г";
                    success = true;
                }
                break;
            case BotState.AdminWaitingForProductDescription:
                product.Description = message.Text;
                fieldName = "описание";
                success = true;
                break;
            case BotState.AdminWaitingForProductCategory:
                product.Category = message.Text;
                fieldName = "категория";
                success = true;
                break;
            case BotState.AdminWaitingForProductCity:
                product.City = message.Text;
                fieldName = "город";
                success = true;
                break;
            case BotState.AdminWaitingForProductStock:
                if (int.TryParse(message.Text, out int stock))
                {
                    product.StockQuantity = stock;
                    fieldName = "количество";
                    success = true;
                }
                break;
            case BotState.AdminWaitingForProductGramPrices:
                var parsedPrices = ParseGramPrices(message.Text ?? "");
                if (parsedPrices.Count > 0)
                {
                    product.GramPrices = parsedPrices;
                    fieldName = "цены за граммовку";
                    success = true;
                }
                break;
        }

        if (success)
        {
            await _context.SaveChangesAsync(ct);
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"✅ {fieldName.ToUpperInvariant()} успешно обновлено!\n\nТовар: {product.Name}",
                cancellationToken: ct);
        }
        else
        {
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "❌ Неверный формат. Попробуйте еще раз:",
                cancellationToken: ct);
            return;
        }

        _adminStateService.ClearEditingState(user.Id);
        await _userService.UpdateUserStateAsync(user.Id, BotState.AdminPanel);
        await ShowProductEditMenuAsync(message.Chat.Id, productId, ct);
    }

    private async Task ShowProductEditMenuAsync(long chatId, int productId, CancellationToken ct)
    {
        var product = await _context.Products.FindAsync(new object?[] { productId }, ct);
        if (product == null) return;

        string status = product.IsActive ? "✅ Активен" : "🚫 Скрыт";
        var pricesText = string.Join("\n", product.GramPrices.Select(p => $"• {p.Key}г - {p.Value}₽"));

        string info = $"📦 **Редактирование товара**\n\n" +
                      $"📝 **Название:** {product.Name}\n" +
                      $"💰 **Цены:**\n{pricesText}\n" +
                      $"🏙️ **Город:** {product.City ?? "Не указан"}\n" +
                      $"🗂️ **Категория:** {product.Category ?? "Не указана"}\n" +
                      $"📦 **Остаток:** {product.StockQuantity} шт.\n" +
                      $"ℹ️ **Описание:** {product.Description ?? "Не указано"}\n" +
                      $"📊 **Статус:** {status}";

        var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(new[]
        {
            new[] { Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData("✏️ Название", $"edit_name_{productId}") },
            new[] { Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData("💰 Цены за граммовку", $"edit_prices_{productId}") },
            new[] { Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData("📝 Описание", $"edit_desc_{productId}") },
            new[] { Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData("🗂️ Категория", $"edit_cat_{productId}") },
            new[] { Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData("🏙️ Город", $"edit_city_{productId}") },
            new[] { Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData("📦 Количество", $"edit_stock_{productId}") },
            new[] {
                Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(product.IsActive ? "🚫 Скрыть" : "✅ Показать", $"toggle_product_{productId}"),
                Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData("🗑️ Удалить", $"delete_product_{productId}")
            },
            new[] { Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData("🔙 Назад к списку", "admin_edit_product") }
        });

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: info,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            replyMarkup: keyboard,
            cancellationToken: ct);
    }
}