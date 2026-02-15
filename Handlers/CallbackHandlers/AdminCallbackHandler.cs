using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using MyShopBotNET9.Models;
using MyShopBotNET9.Services;
using MyShopBotNET9.Handlers.Interfaces;
using MyShopBotNET9.Keyboards;
using MyShopBotNET9.Data;
using Microsoft.EntityFrameworkCore;
using MyUser = MyShopBotNET9.Models.User;
using System.IO;
using Microsoft.Extensions.DependencyInjection;

namespace MyShopBotNET9.Handlers.CallbackHandlers;

public class AdminCallbackHandler : ICallbackHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly AdminService _adminService;
    private readonly UserService _userService;
    private readonly ICatalogService _catalogService;
    private readonly IAdminStateService _adminStateService;
    private readonly AppDbContext _context;
    private readonly IServiceProvider _serviceProvider;

    public AdminCallbackHandler(
        ITelegramBotClient botClient,
        AdminService adminService,
        UserService userService,
        ICatalogService catalogService,
        IAdminStateService adminStateService,
        AppDbContext context,
        IServiceProvider serviceProvider)
    {
        _botClient = botClient;
        _adminService = adminService;
        _userService = userService;
        _catalogService = catalogService;
        _adminStateService = adminStateService;
        _context = context;
        _serviceProvider = serviceProvider;
    }

    public bool CanHandle(string callbackData)
    {
        if (string.IsNullOrEmpty(callbackData)) return false;

        // Эти команды НЕ ДОЛЖНЫ обрабатываться админским хендлером
        if (callbackData == "my_orders" || callbackData == "profile" || callbackData.StartsWith("order_details_"))
        {
            return false;
        }

        // Список всех разрешенных префиксов для админки
        string[] adminPrefixes = {
            "admin_", "edit_", "delete_product_", "toggle_product_", "show_admin",
            "admin_orders_filter_", "admin_order_view_", "admin_confirm_", "admin_ship_", "admin_deliver_",
            "admin_send_photo_", "edit_name_", "edit_price_", "edit_desc_", "edit_cat_", "edit_city_",
            "edit_stock_", "edit_prices_", "admin_support_", "admin_reply_support_", "admin_reply_photo_",
            "admin_support_history_"
        };

        return adminPrefixes.Any(p => callbackData.StartsWith(p));
    }

    public async Task HandleAsync(CallbackQuery callback, MyUser user, CancellationToken ct)
    {
        if (callback.Message == null || string.IsNullOrEmpty(callback.Data)) return;

        if (!AdminConfig.IsAdmin(user.Id))
        {
            Console.WriteLine($"🚨 ПОПЫТКА ВЗЛОМА: Не-админ пытается вызвать админский callback! User ID: {user.Id}, Callback: {callback.Data}");
            await _botClient.AnswerCallbackQueryAsync(callback.Id, "🚨 Доступ запрещен", cancellationToken: ct);
            LogSecurityViolation(user.Id, $"Admin callback attempt: {callback.Data}");
            return;
        }

        var data = callback.Data;

        try { await _botClient.AnswerCallbackQueryAsync(callback.Id, cancellationToken: ct); } catch { }

        switch (data)
        {
            case "admin_orders":
                await ShowOrdersCategoryMenuAsync(callback.Message.Chat.Id, callback.Message.MessageId, ct);
                break;
            case "show_admin":
                await ShowAdminPanelAsync(callback, ct);
                break;
            case "admin_products":
                await ShowProductManagementAsync(callback, ct);
                break;
            case "admin_add_product":
                _adminStateService.ClearProductState(user.Id);
                await _userService.UpdateUserStateAsync(user.Id, BotState.AdminWaitingForProductName);
                await _botClient.SendTextMessageAsync(callback.Message.Chat.Id, "Введите название нового товара:");
                break;
            case "admin_edit_product":
                await ShowProductListForEditAsync(callback, ct);
                break;
            case "admin_support_requests":
                await ShowSupportRequestsAsync(callback.Message.Chat.Id, callback.Message.MessageId, ct);
                break;
            default:
                if (data.StartsWith("edit_product_"))
                {
                    int productId = int.Parse(data.Replace("edit_product_", ""));
                    await ShowProductEditMenuAsync(callback, productId, ct);
                }
                else if (data.StartsWith("admin_orders_filter_"))
                {
                    var status = data.Replace("admin_orders_filter_", "");
                    await ShowOrdersListByFilterAsync(callback.Message.Chat.Id, callback.Message.MessageId, status, ct);
                }
                else if (data.StartsWith("admin_order_view_"))
                {
                    int orderId = int.Parse(data.Replace("admin_order_view_", ""));
                    await ShowOrderDetailAsync(callback.Message.Chat.Id, callback.Message.MessageId, orderId, ct);
                }
                else if (data.StartsWith("admin_confirm_") || data.StartsWith("admin_ship_") || data.StartsWith("admin_deliver_"))
                {
                    await HandleOrderStatusUpdateAsync(callback, user, data, ct);
                }
                else if (data.StartsWith("admin_send_photo_"))
                {
                    int orderId = int.Parse(data.Replace("admin_send_photo_", ""));
                    await StartPhotoUploadAsync(callback, user, orderId, ct);
                }
                else if (data.StartsWith("admin_delivery_time_"))
                {
                    int orderId = int.Parse(data.Replace("admin_delivery_time_", ""));
                    await StartDeliveryTimeInputAsync(callback, user, orderId, ct);
                }
                else if (data.StartsWith("edit_name_") || data.StartsWith("edit_price_") ||
                         data.StartsWith("edit_desc_") || data.StartsWith("edit_cat_") ||
                         data.StartsWith("edit_city_") || data.StartsWith("edit_stock_") ||
                         data.StartsWith("edit_prices_"))
                {
                    await HandleProductFieldEditAsync(callback, user, data, ct);
                }
                else if (data.StartsWith("delete_product_"))
                {
                    int productId = int.Parse(data.Replace("delete_product_", ""));
                    await DeleteProductAsync(callback, productId, ct);
                }
                else if (data.StartsWith("toggle_product_"))
                {
                    int productId = int.Parse(data.Replace("toggle_product_", ""));
                    await ToggleProductActiveAsync(callback, productId, ct);
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
                        await ShowSupportHistoryForAdminAsync(callback.Message.Chat.Id, callback.Message.MessageId, orderId, ct);
                    }
                }
                else if (data == "admin_cancel")
                {
                    await CancelOperationAsync(callback, user, ct);
                }
                break;
        }
    }

    private async Task StartDeliveryTimeInputAsync(CallbackQuery callback, MyUser user, int orderId, CancellationToken ct)
    {
        _adminStateService.SetEditingProductId(user.Id, orderId);
        await _userService.UpdateUserStateAsync(user.Id, BotState.AdminWaitingForDeliveryTime);

        await _botClient.SendTextMessageAsync(
            chatId: callback.Message!.Chat.Id,
            text: $"⏱️ **Введите примерное время доставки для заказа №{orderId}**\n\n" +
                  "Например: '30-40 минут', 'до 18:00', 'завтра с 10 до 14'\n\n" +
                  "Это сообщение будет отправлено клиенту.",
            cancellationToken: ct);
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

    private async Task HandleProductFieldEditAsync(CallbackQuery callback, MyUser user, string data, CancellationToken ct)
    {
        string[] parts = data.Split('_');
        string field = parts[1]; // name, price, desc, cat, city, stock, prices
        int productId = int.Parse(parts[2]);

        _adminStateService.SetEditingProductId(user.Id, productId);

        BotState nextState = field switch
        {
            "name" => BotState.AdminWaitingForProductName,
            "price" => BotState.AdminWaitingForProductPrice,
            "desc" => BotState.AdminWaitingForProductDescription,
            "cat" => BotState.AdminWaitingForProductCategory,
            "city" => BotState.AdminWaitingForProductCity,
            "stock" => BotState.AdminWaitingForProductStock,
            "prices" => BotState.AdminWaitingForProductGramPrices,
            _ => BotState.AdminPanel
        };

        await _userService.UpdateUserStateAsync(user.Id, nextState);

        string prompt = field == "prices"
            ? "⚖️ Введите цены для разных граммовок в формате:\nграмм1:цена1, грамм2:цена2\n\nПример: 0.5:800, 1:1500, 2:2800, 3:4000, 5:6500"
            : "📝 Введите новое значение:";

        await _botClient.SendTextMessageAsync(
            callback.Message!.Chat.Id,
            prompt,
            replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("❌ Отмена", $"edit_product_{productId}")),
            cancellationToken: ct);
    }

    private async Task ShowOrdersCategoryMenuAsync(long chatId, int messageId, CancellationToken ct)
    {
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("🆕 Новые", "admin_orders_filter_Pending") },
            new[] { InlineKeyboardButton.WithCallbackData("✅ Оплаченные", "admin_orders_filter_Confirmed") },
            new[] { InlineKeyboardButton.WithCallbackData("🚚 В пути", "admin_orders_filter_Shipped") },
            new[] { InlineKeyboardButton.WithCallbackData("🎁 Доставленные", "admin_orders_filter_Delivered") },
            new[] { InlineKeyboardButton.WithCallbackData("🔙 Назад в меню", "show_admin") }
        });

        await _botClient.EditMessageTextAsync(chatId, messageId, "📦 **Управление заказами**\nВыберите категорию:",
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: keyboard, cancellationToken: ct);
    }

    private async Task ShowAdminPanelAsync(CallbackQuery callback, CancellationToken ct)
    {
        var stats = await _adminService.GetStatsAsync();
        var statsText = $"📊 **Админ-панель**\n\n" +
                       $"👥 Пользователей: {stats.TotalUsers}\n" +
                       $"📦 Заказов: {stats.TotalOrders}\n" +
                       $"🎁 Товаров: {stats.TotalProducts}\n" +
                       $"💰 Выручка: {stats.TotalRevenue}₽\n" +
                       $"⏳ Ожидающих заказов: {stats.PendingOrders}";

        await _botClient.EditMessageTextAsync(
            callback.Message!.Chat.Id,
            callback.Message.MessageId,
            statsText,
            replyMarkup: AdminKeyboards.GetAdminMainKeyboard(),
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            cancellationToken: ct);
    }

    private async Task ShowProductManagementAsync(CallbackQuery callback, CancellationToken ct)
    {
        await _botClient.EditMessageTextAsync(
            callback.Message!.Chat.Id,
            callback.Message.MessageId,
            "🛍️ Управление товарами:",
            replyMarkup: AdminKeyboards.GetProductManagementKeyboard(),
            cancellationToken: ct);
    }

    private async Task ShowProductListForEditAsync(CallbackQuery callback, CancellationToken ct)
    {
        var products = await _context.Products.OrderBy(p => p.City).ThenBy(p => p.Category).ToListAsync(ct);

        if (!products.Any())
        {
            await _botClient.EditMessageTextAsync(
                callback.Message!.Chat.Id,
                callback.Message.MessageId,
                "📭 Список товаров пуст.",
                replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("🔙 Назад", "admin_products")),
                cancellationToken: ct);
            return;
        }

        var buttons = new List<InlineKeyboardButton[]>();
        foreach (var product in products)
        {
            var statusEmoji = product.IsActive ? "✅" : "❌";
            buttons.Add(new[] {
                InlineKeyboardButton.WithCallbackData(
                    $"{statusEmoji} {product.Name} ({product.City}) - {product.Price}₽",
                    $"edit_product_{product.Id}")
            });
        }

        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Назад", "admin_products") });

        await _botClient.EditMessageTextAsync(
            chatId: callback.Message!.Chat.Id,
            messageId: callback.Message.MessageId,
            text: "🔎 **Выберите товар для редактирования:**",
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            replyMarkup: new InlineKeyboardMarkup(buttons),
            cancellationToken: ct);
    }

    private async Task ShowProductEditMenuAsync(CallbackQuery callback, int productId, CancellationToken ct)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId, ct);
        if (product == null)
        {
            await _botClient.AnswerCallbackQueryAsync(callback.Id, "❌ Товар не найден", cancellationToken: ct);
            return;
        }

        string status = product.IsActive ? "✅ Активен" : "🚫 Скрыт";
        var pricesText = string.Join("\n", product.GramPrices.Select(p => $"• {p.Key}г - {p.Value}₽"));

        string info = $"📦 **Редактирование товара**\n\n" +
                      $"📝 **Название:** {product.Name}\n" +
                      $"💰 **Цены:**\n{pricesText}\n" +
                      $"🏙️ **Город:** {product.City}\n" +
                      $"🗂️ **Категория:** {product.Category}\n" +
                      $"📦 **Остаток:** {product.StockQuantity} шт.\n" +
                      $"ℹ️ **Описание:** {product.Description ?? "Не указано"}\n" +
                      $"📊 **Статус:** {status}";

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("✏️ Название", $"edit_name_{productId}") },
            new[] { InlineKeyboardButton.WithCallbackData("💰 Цены за граммовку", $"edit_prices_{productId}") },
            new[] { InlineKeyboardButton.WithCallbackData("📝 Описание", $"edit_desc_{productId}") },
            new[] { InlineKeyboardButton.WithCallbackData("🗂️ Категория", $"edit_cat_{productId}") },
            new[] { InlineKeyboardButton.WithCallbackData("🏙️ Город", $"edit_city_{productId}") },
            new[] { InlineKeyboardButton.WithCallbackData("📦 Количество", $"edit_stock_{productId}") },
            new[] {
                InlineKeyboardButton.WithCallbackData(product.IsActive ? "🚫 Скрыть" : "✅ Показать", $"toggle_product_{productId}"),
                InlineKeyboardButton.WithCallbackData("🗑️ Удалить", $"delete_product_{productId}")
            },
            new[] { InlineKeyboardButton.WithCallbackData("🔙 Назад к списку", "admin_edit_product") }
        });

        await _botClient.EditMessageTextAsync(
            chatId: callback.Message!.Chat.Id,
            messageId: callback.Message.MessageId,
            text: info,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            replyMarkup: keyboard,
            cancellationToken: ct);
    }

    private async Task ShowOrdersListByFilterAsync(long chatId, int messageId, string statusStr, CancellationToken ct)
    {
        if (!Enum.TryParse(statusStr, out OrderStatus status)) return;

        var orders = await _context.Orders
            .Where(o => o.Status == status)
            .OrderBy(o => o.CreatedAt)
            .ToListAsync(ct);

        if (!orders.Any())
        {
            await _botClient.AnswerCallbackQueryAsync("", "В этой категории нет заказов", cancellationToken: ct);
            return;
        }

        var buttons = orders.Select(o => new[] {
            InlineKeyboardButton.WithCallbackData($"Заказ №{o.Id} | {o.TotalAmount}₽ | {o.CreatedAt:dd.MM HH:mm}", $"admin_order_view_{o.Id}")
        }).ToList();

        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Назад к категориям", "admin_orders") });

        await _botClient.EditMessageTextAsync(
            chatId: chatId,
            messageId: messageId,
            text: $"📋 **Список заказов ({statusStr}):**\n_(Свежие заказы внизу)_",
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            replyMarkup: new InlineKeyboardMarkup(buttons),
            cancellationToken: ct);
    }

    private async Task ShowOrderDetailAsync(long chatId, int messageId, int orderId, CancellationToken ct)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order == null) return;

        var client = order.User;
        string clientInfo = client != null
            ? $"👤 **Клиент:** {client.FirstName}\n🆔 **ID:** `{client.Id}`\n🔗 **Username:** @{client.Username ?? "нет"}"
            : $"👤 **ID Клиента:** `{order.UserId}`";

        string details = $"🆔 **Заказ №{order.Id}**\n" +
                         $"{clientInfo}\n" +
                         $"──────────────────\n" +
                         $"💰 **Сумма:** {order.TotalAmount}₽\n" +
                         $"📍 **Адрес:** {order.Address ?? "Не указан"}\n" +
                         $"📊 **Статус:** {GetStatusEmoji(order.Status)} {order.Status}\n" +
                         $"📸 **Фото:** {(string.IsNullOrEmpty(order.DeliveryPhotoUrl) ? "Нет" : "Есть")}\n\n" +
                         $"🛒 **Товары:**\n" +
                         string.Join("\n", order.OrderItems.Select(i => $"• {i.ProductName} x{i.Quantity} по {i.Price}₽"));

        var keyboard = new InlineKeyboardMarkup(new[] {
        new[] {
            InlineKeyboardButton.WithCallbackData("✅ Оплачено", $"admin_confirm_{order.Id}"),
            InlineKeyboardButton.WithCallbackData("🚚 В пути", $"admin_ship_{order.Id}")
        },
        new[] {
            InlineKeyboardButton.WithCallbackData("🎁 Доставлен", $"admin_deliver_{order.Id}"),
            InlineKeyboardButton.WithCallbackData("⏱️ Время доставки", $"admin_delivery_time_{order.Id}")
        },
        new[] {
            InlineKeyboardButton.WithCallbackData("📸 Отправить фото", $"admin_send_photo_{order.Id}")
        },
        new[] {
            InlineKeyboardButton.WithCallbackData("📬 Поддержка", $"admin_support_history_{order.Id}"),
            InlineKeyboardButton.WithCallbackData("🔙 К списку", $"admin_orders_filter_{order.Status}")
        }
    });

        await _botClient.EditMessageTextAsync(chatId, messageId, details,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            replyMarkup: keyboard, cancellationToken: ct);
    }

    private async Task HandleOrderStatusUpdateAsync(CallbackQuery callback, MyUser user, string data, CancellationToken ct)
    {
        var parts = data.Split('_');
        string action = parts[1];
        int orderId = int.Parse(parts[2]);

        var order = await _context.Orders.FindAsync(new object[] { orderId }, ct);
        if (order == null) return;

        string statusTxt = "";
        switch (action)
        {
            case "confirm":
                order.Status = OrderStatus.Confirmed;
                statusTxt = "✅ Оплачен";
                break;
            case "ship":
                order.Status = OrderStatus.Shipped;
                statusTxt = "🚚 В пути";
                break;
            case "deliver":
                order.Status = OrderStatus.Delivered;
                statusTxt = "🎁 Доставлен";
                break;
        }

        await _context.SaveChangesAsync(ct);
        LogAdminAction(user.Id, $"Changed order {orderId} status to {statusTxt}");

        try
        {
            string clientMsg = $"🔔 **Обновление по заказу №{order.Id}**\nНовый статус: **{statusTxt}**";
            await _botClient.SendTextMessageAsync(order.UserId, clientMsg, parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown, cancellationToken: ct);
        }
        catch { }

        await _botClient.EditMessageTextAsync(
            chatId: callback.Message!.Chat.Id,
            messageId: callback.Message.MessageId,
            text: callback.Message.Text + $"\n\n⚙️ **Статус изменен на: {statusTxt}**",
            replyMarkup: callback.Message.ReplyMarkup,
            cancellationToken: ct);

        await _botClient.AnswerCallbackQueryAsync(callback.Id, $"Заказ #{orderId}: {statusTxt}", cancellationToken: ct);
    }

    private async Task StartPhotoUploadAsync(CallbackQuery callback, MyUser user, int orderId, CancellationToken ct)
    {
        _adminStateService.SetEditingProductId(user.Id, orderId);
        await _userService.UpdateUserStateAsync(user.Id, BotState.AdminWaitingForProductPhoto);
        await _botClient.SendTextMessageAsync(callback.Message!.Chat.Id, $"📸 **Отправьте фото для заказа №{orderId}**\n\nПожалуйста, отправьте фотографию товара/доставки как изображение (не файл).", cancellationToken: ct);
    }

    private async Task DeleteProductAsync(CallbackQuery callback, int productId, CancellationToken ct)
    {
        var product = await _catalogService.GetProductByIdForAdminAsync(productId);
        if (product != null)
        {
            _context.Products.Remove(product);
            await _context.SaveChangesAsync(ct);
            LogAdminAction(callback.From.Id, $"Deleted product: {product.Name} (ID: {productId})");
        }
        await _botClient.AnswerCallbackQueryAsync(callback.Id, "🗑️ Удалено", cancellationToken: ct);
        await ShowProductListForEditAsync(callback, ct);
    }

    private async Task ToggleProductActiveAsync(CallbackQuery callback, int productId, CancellationToken ct)
    {
        var product = await _catalogService.GetProductByIdForAdminAsync(productId);
        if (product != null)
        {
            product.IsActive = !product.IsActive;
            await _adminService.UpdateProductAsync(product);
            LogAdminAction(callback.From.Id, $"Toggled product {productId} active to: {product.IsActive}");
            await ShowProductEditMenuAsync(callback, productId, ct);
        }
    }

    private async Task CancelOperationAsync(CallbackQuery callback, MyUser user, CancellationToken ct)
    {
        _adminStateService.ClearProductState(user.Id);
        _adminStateService.ClearEditingState(user.Id);
        await _userService.UpdateUserStateAsync(user.Id, BotState.AdminPanel);
        await ShowAdminPanelAsync(callback, ct);
    }

    private async Task ShowSupportRequestsAsync(long chatId, int messageId, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var supportService = scope.ServiceProvider.GetRequiredService<SupportService>();
        var orderService = scope.ServiceProvider.GetRequiredService<OrderService>();

        var ordersWithUnread = await supportService.GetOrdersWithUnreadMessagesAsync();

        if (!ordersWithUnread.Any())
        {
            await _botClient.EditMessageTextAsync(
                chatId: chatId,
                messageId: messageId,
                text: "📭 **Нет новых обращений в поддержку**",
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                replyMarkup: new InlineKeyboardMarkup(new[]
                {
                    new[] { InlineKeyboardButton.WithCallbackData("🔙 Назад", "show_admin") }
                }),
                cancellationToken: ct);
            return;
        }

        var buttons = new List<InlineKeyboardButton[]>();

        foreach (var orderId in ordersWithUnread.Take(10))
        {
            var order = await orderService.GetOrderByIdAsync(orderId);
            if (order != null)
            {
                var user = await _userService.GetUserAsync(order.UserId);
                var clientName = user?.FirstName ?? $"Клиент {order.UserId}";

                buttons.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        $"📩 Заказ №{orderId} - {clientName} (🔴 новое)",
                        $"admin_support_history_{orderId}")
                });
            }
        }

        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Назад", "show_admin") });

        await _botClient.EditMessageTextAsync(
            chatId: chatId,
            messageId: messageId,
            text: "📬 **Обращения в поддержку**\n\nВыберите заказ для ответа:",
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            replyMarkup: new InlineKeyboardMarkup(buttons),
            cancellationToken: ct);
    }

    private async Task ShowSupportHistoryForAdminAsync(long chatId, int messageId, int orderId, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var supportService = scope.ServiceProvider.GetRequiredService<SupportService>();
        var orderService = scope.ServiceProvider.GetRequiredService<OrderService>();

        var messages = await supportService.GetOrderChatHistoryAsync(orderId);
        var order = await orderService.GetOrderByIdAsync(orderId);
        var user = await _userService.GetUserAsync(order?.UserId ?? 0);

        if (order == null || user == null) return;

        // Отмечаем сообщения как прочитанные админом
        await supportService.MarkMessagesAsReadByAdminAsync(orderId);

        if (!messages.Any())
        {
            await _botClient.EditMessageTextAsync(
                chatId: chatId,
                messageId: messageId,
                text: $"💬 **История поддержки по заказу №{orderId}**\n\nПока нет сообщений.",
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                replyMarkup: GetSupportHistoryKeyboard(orderId, order.UserId),
                cancellationToken: ct);
            return;
        }

        var chatHistory = $"💬 **История поддержки по заказу №{orderId}**\n";
        chatHistory += $"👤 Клиент: {user.FirstName} (@{user.Username ?? "нет"})\n";
        chatHistory += $"──────────────────\n\n";

        foreach (var msg in messages.OrderBy(m => m.SentAt))
        {
            var sender = msg.SenderType == SenderType.Client ? "👤 Клиент" : "👨‍💼 Админ";
            var time = msg.SentAt.ToString("dd.MM HH:mm");

            if (!string.IsNullOrEmpty(msg.PhotoFileId))
            {
                chatHistory += $"{sender} [{time}]: 📸 Фото\n";

                // Отправляем фото отдельным сообщением, если их немного
                if (messages.Count(m => !string.IsNullOrEmpty(m.PhotoFileId)) <= 3)
                {
                    try
                    {
                        await _botClient.SendPhotoAsync(
                            chatId: chatId,
                            photo: InputFile.FromFileId(msg.PhotoFileId),
                            caption: $"{sender} [{time}]",
                            cancellationToken: ct);
                    }
                    catch { }
                }
            }
            else if (!string.IsNullOrEmpty(msg.MessageText))
            {
                chatHistory += $"{sender} [{time}]: {msg.MessageText}\n";
            }
        }

        var keyboard = GetSupportHistoryKeyboard(orderId, order.UserId);

        // Если сообщение слишком длинное, разбиваем
        if (chatHistory.Length > 4000)
        {
            var firstPart = chatHistory.Substring(0, 3500) + "...\n\n(продолжение в следующем сообщении)";

            await _botClient.EditMessageTextAsync(
                chatId: chatId,
                messageId: messageId,
                text: firstPart,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                cancellationToken: ct);

            var secondPart = chatHistory.Substring(3500);

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: secondPart,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                replyMarkup: keyboard,
                cancellationToken: ct);
        }
        else
        {
            await _botClient.EditMessageTextAsync(
                chatId: chatId,
                messageId: messageId,
                text: chatHistory,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                replyMarkup: keyboard,
                cancellationToken: ct);
        }
    }

    private InlineKeyboardMarkup GetSupportHistoryKeyboard(int orderId, long clientId)
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📝 Ответить текстом", $"admin_reply_support_{orderId}_{clientId}"),
                InlineKeyboardButton.WithCallbackData("📸 Ответить фото", $"admin_reply_photo_{orderId}_{clientId}")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🔙 К списку обращений", "admin_support_requests"),
                InlineKeyboardButton.WithCallbackData("🔙 К заказу", $"admin_order_view_{orderId}")
            }
        });
    }

    private string GetStatusEmoji(OrderStatus status) => status switch
    {
        OrderStatus.Pending => "⏳",
        OrderStatus.Confirmed => "✅",
        OrderStatus.Shipped => "🚚",
        OrderStatus.Delivered => "🎁",
        OrderStatus.Completed => "🏁",
        OrderStatus.Cancelled => "❌",
        _ => "📝"
    };

    private void LogSecurityViolation(long userId, string violationDetails)
    {
        try
        {
            string logMessage = $"[SECURITY VIOLATION] {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} - User ID: {userId} - {violationDetails}";
            Console.WriteLine($"🚨 {logMessage}");
            System.IO.File.AppendAllText("security_violations.log", logMessage + Environment.NewLine);
        }
        catch { }
    }

    private void LogAdminAction(long adminId, string action)
    {
        try
        {
            string logMessage = $"[ADMIN ACTION] {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} - Admin ID: {adminId} - {action}";
            Console.WriteLine($"📝 {logMessage}");
            System.IO.File.AppendAllText("admin_actions.log", logMessage + Environment.NewLine);
        }
        catch { }
    }
}