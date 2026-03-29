using BLL.DTOs;
using DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.IService
{
    public interface IOrderService
    {
        Task<OrderDto?> GetOrderByIdAsync(int orderId, int userId);
        Task<List<OrderDto>> GetUserOrdersAsync(int userId);
        Task<List<OrderDto>> GetAllOrdersAsync();
        Task<OrderDto> CreateOrderAsync(CreateOrderDto dto);
        Task<OrderDto> CreateOrderBuyNowAsync(int userId, int productId, int quantity, CreateOrderDto dto);
        Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus);
        Task<bool> CancelOrderAsync(int orderId, int userId);
        // Shipper flow
        Task<bool> AssignShipperAsync(int orderId, int shipperId, string? trackingNumber, string? carrier);
        Task<bool> MarkDeliveredAsync(int orderId, int shipperId);
        Task<List<OrderDto>> GetShipperOrdersAsync(int shipperId);
        Task<OrderDto?> GetOrderByIdForAdminAsync(int orderId);
        // Khách hàng xác nhận đã/chưa nhận hàng (khi status = Delivered)
        Task<bool> ConfirmReceivedByCustomerAsync(int orderId, int userId);
        Task<bool> ReportNotReceivedByCustomerAsync(int orderId, int userId);
    }
}
