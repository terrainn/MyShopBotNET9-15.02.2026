using MyShopBotNET9.Models;

namespace MyShopBotNET9.Services;

public interface IOrderService
{
    Task<Order> CreateOrderAsync(long userId, List<CartItem> cartItems, string address);
    Task<List<Order>> GetUserOrdersAsync(long userId);
    Task<Order?> GetOrderByIdAsync(int orderId);
    Task UpdateOrderAsync(Order order);
    Task<List<Order>> GetPendingOrdersAsync();
    Task<List<Order>> GetAllOrdersAsync();
    Task<bool> CancelOrderAsync(int orderId, long userId); // днаюбкемн
}