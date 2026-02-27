using BLL.IService;
using DAL.Entities;
using DAL.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Service
{
    public class CartService : ICartService
    {
        private readonly ICartRepository _cartRepository;

        public CartService(ICartRepository cartRepository)
        {
            _cartRepository = cartRepository;
        }

        public Cart GetCart(int userId)
            => _cartRepository.GetCartByUserId(userId);

        public void AddItem(int userId, int productId, int quantity)
            => _cartRepository.AddItem(userId, productId, quantity);

        public void UpdateItem(int cartItemId, int quantity)
            => _cartRepository.UpdateQuantity(cartItemId, quantity);

        public void RemoveItem(int cartItemId)
            => _cartRepository.RemoveItem(cartItemId);

        public Task AddOrReplaceSingleItemAsync(int userId, int productId, int quantity)
            => _cartRepository.AddOrReplaceSingleItemAsync(userId, productId, quantity);
    }
}
