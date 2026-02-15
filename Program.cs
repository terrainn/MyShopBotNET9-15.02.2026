using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyShopBotNET9.Data;
using MyShopBotNET9.Handlers;
using MyShopBotNET9.Handlers.CallbackHandlers;
using MyShopBotNET9.Handlers.MessageHandlers;
using MyShopBotNET9.Handlers.Interfaces;
using MyShopBotNET9.Services;
using Telegram.Bot;
using Microsoft.Extensions.Configuration;

var builder = Host.CreateApplicationBuilder(args);

// Временно создаем клиента для принудительной очистки (только если не в контейнере или для теста)
try
{
    var tempBotClient = new TelegramBotClient("8438099672:AAFi1sCFIiFa-Fz8nFheypmVecJajrHhbo8");

    // Удаляем вебхук (на всякий случай)
    await tempBotClient.DeleteWebhookAsync();
    Console.WriteLine("✅ Webhook deleted");

    // Получаем информацию о вебхуке
    var webhookInfo = await tempBotClient.GetWebhookInfoAsync();
    Console.WriteLine($"📊 Webhook info: URL='{webhookInfo.Url}', Pending updates={webhookInfo.PendingUpdateCount}");

    // Принудительно сбрасываем все ожидающие обновления
    var updates = await tempBotClient.GetUpdatesAsync(offset: -1, timeout: 1);
    Console.WriteLine($"🔄 Dropped {updates.Length} pending updates");

    Console.WriteLine("✅ Все конфликты очищены");
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ Ошибка при очистке: {ex.Message}");
}

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true);
// .AddEnvironmentVariables(); // Добавь если нужно

// --- 1. Конфигурация бота ---
var botToken = builder.Configuration["TelegramBot:Token"];
if (string.IsNullOrEmpty(botToken))
{
    throw new Exception("Telegram Bot Token is missing in configuration!");
}
builder.Services.AddSingleton<ITelegramBotClient>(_ => new TelegramBotClient(botToken));

// --- остальной код без изменений ---

// --- 2. База данных (SQLite) ---
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=myshopbot.db"), ServiceLifetime.Scoped);

// --- 3. Основные бизнес-сервисы ---
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddScoped<CatalogService>();
builder.Services.AddScoped<ICatalogService, CatalogService>();

builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<ICartService, CartService>();

builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<IOrderService, OrderService>();

builder.Services.AddScoped<CityService>();
builder.Services.AddScoped<ICityService, CityService>();

builder.Services.AddScoped<AdminService>();  // ← ЭТА СТРОЧКА ДОЛЖНА БЫТЬ
builder.Services.AddScoped<NotificationService>();

// Singleton сервисы для хранения состояний в памяти
builder.Services.AddSingleton<PendingOrderService>();
builder.Services.AddSingleton<IAdminStateService, AdminStateServiceNew>();

// --- 4. Регистрация Callback Handlers (Кнопки) ---
// ВАЖНО: Регистрируем только как ICallbackHandler для корректной работы IEnumerable в TelegramBotHandler
builder.Services.AddScoped<ICallbackHandler, AdminOrderCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, AdminCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, CatalogCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, CartCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, OrderCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, CityCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, PaymentCallbackHandler>();

// --- 5. Регистрация Message Handlers (Текст) ---
// ВАЖНО: Регистрируем только как IMessageHandler
builder.Services.AddScoped<IMessageHandler, CatalogMessageHandler>();
builder.Services.AddScoped<IMessageHandler, CartMessageHandler>();
builder.Services.AddScoped<IMessageHandler, OrderMessageHandler>();
builder.Services.AddScoped<IMessageHandler, ProfileMessageHandler>();
builder.Services.AddScoped<IMessageHandler, PaymentMessageHandler>();
builder.Services.AddScoped<IMessageHandler, AdminMessageHandler>();
builder.Services.AddScoped<IMessageHandler, AdminProductMessageHandler>();
builder.Services.AddScoped<AdminPhotoMessageHandler>();
// Добавить в секцию регистрации сервисов:
builder.Services.AddScoped<SupportService>();

// Добавить в секцию регистрации обработчиков:
builder.Services.AddScoped<IMessageHandler, SupportMessageHandler>();
builder.Services.AddScoped<ICallbackHandler, SupportCallbackHandler>();
builder.Services.AddScoped<IMessageHandler, AdminDeliveryTimeMessageHandler>(); // ← добавить
builder.Services.AddScoped<IMessageHandler, AdminPhotoMessageHandler>(); // ← ЭТА СТРОЧКА

// --- 6. Главные службы управления ботом ---
builder.Services.AddScoped<TelegramBotHandler>();
builder.Services.AddSingleton<TelegramBotService>();

var host = builder.Build();

// Автоматическое применение миграций при запуске
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    // Установка начального номера заказа
    await ResetOrderIdSequence(db);
}

// Запуск Telegram-сервиса
var botService = host.Services.GetRequiredService<TelegramBotService>();
await botService.StartBotAsync();

await host.RunAsync();

// Метод для установки начального номера заказа
static async Task ResetOrderIdSequence(AppDbContext db)
{
    try
    {
        // Проверяем, есть ли уже заказы
        var orderCount = await db.Orders.CountAsync();

        if (orderCount == 0)
        {
            // Если заказов нет, устанавливаем начальный ID
            await db.Database.ExecuteSqlRawAsync(
                "INSERT OR REPLACE INTO sqlite_sequence (name, seq) VALUES ('Orders', 20353)");

            Console.WriteLine("✅ Начальный номер заказа установлен на 20354");
        }
        else
        {
            // Если заказы уже есть, находим максимальный ID
            var maxId = await db.Orders.MaxAsync(o => (int?)o.Id) ?? 0;

            if (maxId < 20354)
            {
                // Устанавливаем следующий ID
                await db.Database.ExecuteSqlRawAsync(
                    "UPDATE sqlite_sequence SET seq = 20353 WHERE name = 'Orders'");

                Console.WriteLine($"✅ Следующий номер заказа будет: 20354 (текущий max: {maxId})");
            }
            else
            {
                Console.WriteLine($"✅ Текущий максимальный номер заказа: {maxId}");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Ошибка установки номера заказа: {ex.Message}");
    }
}