using BLL.DTOs;
using BLL.Helper;
using DAL.Entities;

namespace BLL.IService
{
    public interface IWishlistService
    {
        Task<GenericResult<IEnumerable<WishlistProductDTO>>> GetUserWishlistAsync();
        Task<GenericResult<bool>> AddToWishlistAsync(int productId, string? note = null);
        Task<GenericResult<bool>> RemoveFromWishlistAsync(int wishlistProductId);
        Task<GenericResult<int>> GetWishlistCountAsync();
        Task<GenericResult<bool>> ClearWishlistAsync();
        Task<GenericResult<IEnumerable<WishlistProduct>>> GetAllWishlistProductsForAdminAsync();
        Task<bool> IsProductInWishlistAsync(int productId);
        Task<GenericResult<bool>> ToggleWishlistAsync(int productId);
        Task<GenericResult<bool>> CreateEmptyWishlistForUserAsync(int userId, string? note = null);

        Task<bool> IsInWishlistAsync(int userId, int productId);
        Task ToggleWishlistAsync(int userId, int productId);
    }
}
