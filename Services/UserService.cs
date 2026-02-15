using Microsoft.EntityFrameworkCore;
using MyShopBotNET9.Data;
using MyShopBotNET9.Models;

namespace MyShopBotNET9.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _context;

    public UserService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User> GetOrCreateUserAsync(long userId, string? firstName)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                // ПРОВЕРЯЕМ АДМИНСКИЙ СТАТУС ИЗ КОНФИГУРАЦИИ
                bool isAdmin = AdminConfig.IsAdmin(userId);

                user = new User
                {
                    Id = userId,
                    FirstName = firstName,
                    Username = firstName,
                    CurrentState = BotState.MainMenu,
                    CreatedAt = DateTime.UtcNow,
                    LastActivity = DateTime.UtcNow,
                    IsAdmin = isAdmin // Устанавливаем статус из конфига
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                Console.WriteLine($"✅ Создан {(isAdmin ? "АДМИН " : "")}пользователь: {userId}");
            }
            else
            {
                // Обновляем статус админа из конфига при каждом входе
                bool shouldBeAdmin = AdminConfig.IsAdmin(userId);
                if (user.IsAdmin != shouldBeAdmin)
                {
                    user.IsAdmin = shouldBeAdmin;
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"🔄 Обновлен админский статус для {userId}: {shouldBeAdmin}");
                }

                user.LastActivity = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return user;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error in GetOrCreateUserAsync: {ex.Message}");
            return new User
            {
                Id = userId,
                FirstName = firstName,
                Username = firstName,
                CurrentState = BotState.MainMenu,
                IsAdmin = AdminConfig.IsAdmin(userId) // Важно!
            };
        }
    }

    public async Task UpdateUserStateAsync(long userId, BotState state)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.CurrentState = state;
                user.LastActivity = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error updating user state: {ex.Message}");
        }
    }

    public async Task UpdateUserCityAsync(long userId, string city)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.City = city;
                user.LastActivity = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error updating user city: {ex.Message}");
        }
    }

    public async Task<User?> GetUserAsync(long userId)
    {
        try
        {
            Console.WriteLine($"🔍 Getting user {userId} with cart...");
            var user = await _context.Users
                .Include(u => u.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(u => u.Id == userId);

            Console.WriteLine($"📋 User found: {(user != null ? "YES" : "NO")}");
            return user;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error getting user: {ex.Message}");
            return null;
        }
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        try
        {
            return await _context.Users.ToListAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error getting all users: {ex.Message}");
            return new List<User>();
        }
    }
    public async Task<User?> GetUserBasicAsync(long userId)
    {
        try
        {
            return await _context.Users.FindAsync(userId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error getting basic user: {ex.Message}");
            return null;
        }
    }

    public async Task<User?> GetUserWithCartAsync(long userId)
    {
        try
        {
            return await _context.Users
                .Include(u => u.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error getting user with cart: {ex.Message}");
            return null;
        }
    }

    public async Task<User?> GetUserByChatId(long chatId)
    {
        // В нашей модели User.Id и есть chatId
        return await GetUserAsync(chatId);
    }
    public async Task<bool> IsUserAdminAsync(long userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            return user?.IsAdmin ?? false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error checking admin status for user {userId}: {ex.Message}");
            return false;
        }
    }
    public async Task<List<User>> GetAdminsAsync()
    {
        return await _context.Users
            .Where(u => u.IsAdmin)
            .ToListAsync();
    }
    public async Task<bool> CheckAndUpdateAdminStatusAsync(long userId)
    {
        try
        {
            var user = await GetUserAsync(userId);
            if (user == null) return false;

            bool shouldBeAdmin = AdminConfig.IsAdmin(userId);

            if (user.IsAdmin != shouldBeAdmin)
            {
                user.IsAdmin = shouldBeAdmin;
                await _context.SaveChangesAsync();
                Console.WriteLine($"🔄 Обновлен админский статус для {userId}: {shouldBeAdmin}");
            }

            return shouldBeAdmin;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error checking admin status: {ex.Message}");
            return false;
        }
    }
}