using DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.IService
{
    public interface ICartService
    {
        Cart GetCart(int userId);
        void AddItem(int userId, int productId, int quantity);
        void UpdateItem(int cartItemId, int quantity);
        void RemoveItem(int cartItemId);
        Task AddOrReplaceSingleItemAsync(int userId, int productId, int quantity);
    }
}
