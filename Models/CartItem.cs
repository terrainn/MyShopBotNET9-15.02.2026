using System.ComponentModel.DataAnnotations.Schema;

namespace MyShopBotNET9.Models;

public class CartItem
{
    public int Id { get; set; }
    public long UserId { get; set; }
    public int ProductId { get; set; }
    public decimal SelectedGram { get; set; } = 1.0m;
    public int Quantity { get; set; } = 1;
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("UserId")]
    public User User { get; set; } = null!;

    [ForeignKey("ProductId")]
    public Product Product { get; set; } = null!;

    [NotMapped]
    public decimal TotalPrice
    {
        get
        {
            if (Product == null) return 0;

            // Добавьте отладочный вывод
            Console.WriteLine($"💰 Calculating price for {Product.Name}, SelectedGram={SelectedGram}");
            Console.WriteLine($"   GramPrices: {string.Join(", ", Product.GramPrices.Select(kv => $"{kv.Key}:{kv.Value}"))}");

            if (Product.GramPrices != null && Product.GramPrices.ContainsKey(SelectedGram))
            {
                var price = Product.GramPrices[SelectedGram];
                Console.WriteLine($"   Found price for {SelectedGram}г: {price}");
                return price * Quantity;
            }

            Console.WriteLine($"   Using default price: {Product.Price}");
            return Product.Price * Quantity;
        }
    }
}