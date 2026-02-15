using MyShopBotNET9.Models;

namespace MyShopBotNET9.Services
{
    public class PendingOrderService
    {
        private readonly Dictionary<long, PendingOrder> _pendingOrders = new();
        private readonly object _lock = new object();

        public class PendingOrder
        {
            public string? District { get; set; }
            public decimal TotalAmount { get; set; }
            public List<CartItem>? CartItems { get; set; }
            public string? UserCity { get; set; }
            public string? UserName { get; set; }
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        }

        public void StartOrder(long userId, decimal totalAmount, List<CartItem> cartItems, string userCity, string userName)
        {
            lock (_lock)
            {
                _pendingOrders[userId] = new PendingOrder
                {
                    TotalAmount = totalAmount,
                    CartItems = cartItems,
                    UserCity = userCity,
                    UserName = userName,
                    CreatedAt = DateTime.UtcNow
                };

                Console.WriteLine($"✅ PendingOrder saved for user {userId}. Total: {totalAmount}₽, Items: {cartItems.Count}");

                // Очищаем старые заказы (старше 1 часа)
                CleanupOldOrders();
            }
        }

        public void SetDistrict(long userId, string district)
        {
            lock (_lock)
            {
                if (_pendingOrders.ContainsKey(userId))
                {
                    _pendingOrders[userId].District = district;
                    Console.WriteLine($"📍 District set for user {userId}: {district}");
                }
                else
                {
                    Console.WriteLine($"❌ No pending order found for user {userId} when setting district");
                }
            }
        }

        public PendingOrder? GetOrder(long userId)
        {
            lock (_lock)
            {
                if (_pendingOrders.ContainsKey(userId))
                {
                    var order = _pendingOrders[userId];
                    Console.WriteLine($"✅ Found pending order for user {userId}. District: {order.District}, Total: {order.TotalAmount}₽");
                    return order;
                }
                else
                {
                    Console.WriteLine($"❌ No pending order found for user {userId}. Available orders: {string.Join(", ", _pendingOrders.Keys)}");
                    return null;
                }
            }
        }

        public void ClearOrder(long userId)
        {
            lock (_lock)
            {
                _pendingOrders.Remove(userId);
                Console.WriteLine($"🗑️ Cleared pending order for user {userId}");
            }
        }

        private void CleanupOldOrders()
        {
            var cutoff = DateTime.UtcNow.AddHours(-1);
            var oldOrders = _pendingOrders.Where(kvp => kvp.Value.CreatedAt < cutoff).ToList();

            foreach (var oldOrder in oldOrders)
            {
                _pendingOrders.Remove(oldOrder.Key);
                Console.WriteLine($"🧹 Cleaned up old pending order for user {oldOrder.Key}");
            }
        }
    }
}