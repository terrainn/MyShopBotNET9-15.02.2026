using MyShopBotNET9.Models;

namespace MyShopBotNET9.Services;

public interface IAdminStateService
{
    void StartProductCreation(long adminId);
    void SaveProductName(long adminId, string name);
    void SaveProductPrice(long adminId, decimal price);
    void SaveProductDescription(long adminId, string description);
    void SaveProductStock(long adminId, int stock);
    void SaveProductCategory(long adminId, string category);
    void SaveProductCity(long adminId, string city);
    void SaveProductImage(long adminId, string imageUrl);
    AdminAddProductState? GetProductState(long adminId);
    void ClearProductState(long adminId);
    Product CreateProductFromState(long adminId);

    void SetEditingProductId(long adminId, int productId);
    int? GetEditingProductId(long adminId);
    void ClearEditingState(long adminId);
}