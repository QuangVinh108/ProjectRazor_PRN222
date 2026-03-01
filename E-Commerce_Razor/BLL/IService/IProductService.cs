using BLL.DTOs;
using DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.IService
{
    public interface IProductService
    {
        IEnumerable<ProductViewModel> GetAll();
        List<ProductViewModel> GetFilteredProducts(string searchTerm, int? categoryId, decimal? minPrice, decimal? maxPrice, string sortOrder);
        CreateProductViewModel GetById(int id);
        int Create(CreateProductViewModel model);
        void Update(CreateProductViewModel model);
        void Delete(int id);
        ProductViewModel GetDetail(int id);
        List<ProductViewModel> GetProductsForAdmin(int? parentId, int? categoryId);
    }
}
