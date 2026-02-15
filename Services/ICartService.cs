using MyShopBotNET9.Models;

namespace MyShopBotNET9.Services;

public interface ICartService
{
    Task AddToCartAsync(long userId, int productId, int quantity, decimal selectedGram = 1.0m);
    Task<List<CartItem>> GetCartItemsAsync(long userId);
    Task UpdateCartItemQuantityAsync(long userId, int productId, int quantity, decimal? selectedGram = null);
    Task RemoveFromCartAsync(long userId, int productId, decimal? selectedGram = null);
    Task ClearCartAsync(long userId);
    Task<decimal> GetCartTotalAsync(long userId);
    Task<int> GetCartItemsCountAsync(long userId);
    Task<bool> IsProductInCartAsync(long userId, int productId, decimal? selectedGram = null);
}