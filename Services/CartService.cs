using Microsoft.EntityFrameworkCore;
using MyShopBotNET9.Data;
using MyShopBotNET9.Models;

namespace MyShopBotNET9.Services;

public class CartService : ICartService
{
    private readonly AppDbContext _context;

    public CartService(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddToCartAsync(long userId, int productId, int quantity, decimal selectedGram = 1.0m)
    {
        try
        {
            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.UserId == userId &&
                                          ci.ProductId == productId &&
                                          ci.SelectedGram == selectedGram);

            if (cartItem != null)
            {
                cartItem.Quantity += quantity;
            }
            else
            {
                cartItem = new CartItem
                {
                    UserId = userId,
                    ProductId = productId,
                    Quantity = quantity,
                    SelectedGram = selectedGram
                };
                _context.CartItems.Add(cartItem);
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error adding to cart: {ex.Message}");
            throw;
        }
    }

    public async Task<List<CartItem>> GetCartItemsAsync(long userId)
    {
        try
        {
            return await _context.CartItems
                .Include(ci => ci.Product)
                .Where(ci => ci.UserId == userId)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error getting cart items: {ex.Message}");
            return new List<CartItem>();
        }
    }

    public async Task UpdateCartItemQuantityAsync(long userId, int productId, int quantity, decimal? selectedGram = null)
    {
        try
        {
            var query = _context.CartItems
                .Where(ci => ci.UserId == userId && ci.ProductId == productId);

            if (selectedGram.HasValue)
                query = query.Where(ci => ci.SelectedGram == selectedGram.Value);

            var cartItem = await query.FirstOrDefaultAsync();

            if (cartItem != null)
            {
                if (quantity <= 0)
                {
                    _context.CartItems.Remove(cartItem);
                }
                else
                {
                    cartItem.Quantity = quantity;
                }
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error updating cart item: {ex.Message}");
            throw;
        }
    }

    public async Task RemoveFromCartAsync(long userId, int productId, decimal? selectedGram = null)
    {
        try
        {
            var query = _context.CartItems
                .Where(ci => ci.UserId == userId && ci.ProductId == productId);

            if (selectedGram.HasValue)
                query = query.Where(ci => ci.SelectedGram == selectedGram.Value);

            var cartItem = await query.FirstOrDefaultAsync();

            if (cartItem != null)
            {
                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error removing from cart: {ex.Message}");
            throw;
        }
    }

    public async Task ClearCartAsync(long userId)
    {
        try
        {
            var cartItems = await _context.CartItems
                .Where(ci => ci.UserId == userId)
                .ToListAsync();

            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error clearing cart: {ex.Message}");
            throw;
        }
    }

    public async Task<decimal> GetCartTotalAsync(long userId)
    {
        try
        {
            var items = await GetCartItemsAsync(userId);
            return items.Sum(i => i.TotalPrice);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error calculating cart total: {ex.Message}");
            return 0;
        }
    }

    public async Task<int> GetCartItemsCountAsync(long userId)
    {
        try
        {
            return await _context.CartItems
                .Where(ci => ci.UserId == userId)
                .SumAsync(ci => ci.Quantity);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error getting cart items count: {ex.Message}");
            return 0;
        }
    }

    public async Task<bool> IsProductInCartAsync(long userId, int productId, decimal? selectedGram = null)
    {
        try
        {
            var query = _context.CartItems
                .Where(ci => ci.UserId == userId && ci.ProductId == productId);

            if (selectedGram.HasValue)
                query = query.Where(ci => ci.SelectedGram == selectedGram.Value);

            return await query.AnyAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error checking product in cart: {ex.Message}");
            return false;
        }
    }
}