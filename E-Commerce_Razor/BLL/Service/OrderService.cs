using BLL.DTOs;
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
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepo;
        private readonly ICartRepository _cartRepo;
        private readonly IInventoryService _inventoryService;

        public OrderService(IOrderRepository orderRepo, ICartRepository cartRepo, IInventoryService inventoryService)
        {
            _orderRepo = orderRepo;
            _cartRepo = cartRepo;
            _inventoryService = inventoryService;
        }

        public async Task<OrderDto?> GetOrderByIdAsync(int orderId, int userId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId, includeDetails: true);
            // Check quyền sở hữu
            if (order == null || order.UserId != userId) return null;
            return MapToOrderDto(order);
        }

        public async Task<List<OrderDto>> GetUserOrdersAsync(int userId)
        {
            var orders = await _orderRepo.GetByUserIdAsync(userId);
            return orders.Select(MapToOrderDto).ToList();
        }

        public async Task<List<OrderDto>> GetAllOrdersAsync()
        {
            var orders = await _orderRepo.GetAllAsync();
            return orders.Select(MapToOrderDto).ToList();
        }

        public async Task<OrderDto> CreateOrderAsync(CreateOrderDto dto)
        {
            var cart = await _cartRepo.GetByUserIdAsync(dto.UserId);
            if (cart == null || !cart.CartItems.Any()) throw new Exception("Giỏ hàng trống.");

            decimal totalAmount = cart.CartItems.Sum(ci => ci.Quantity * ci.UnitPrice);

            var order = new Order
            {
                UserId = dto.UserId,
                OrderDate = DateTime.Now,
                Status = "Pending", // Hardcode string
                TotalAmount = totalAmount,
                Note = dto.Note,
                IsActive = true,
                OrderItems = cart.CartItems.Select(ci => new OrderItem
                {
                    ProductId = ci.ProductId,
                    Quantity = ci.Quantity,
                    UnitPrice = ci.UnitPrice,
                    // Image = ci.Product.Image
                }).ToList(),
                Payment = new Payment
                {
                    PaymentMethod = dto.PaymentMethod,
                    Amount = totalAmount,
                    Status = "Pending" // Hardcode string
                },
                Shipping = new Shipping
                {
                    Address = dto.ShippingAddress,
                    City = dto.City,
                    Country = dto.Country,
                    PostalCode = dto.PostalCode
                }
            };

            var createdOrder = await _orderRepo.CreateAsync(order);
            await _cartRepo.ClearCartAsync(dto.UserId);

            return MapToOrderDto(await _orderRepo.GetByIdAsync(createdOrder.OrderId, true)!);
        }
        public async Task<OrderDto> CreateOrderBuyNowAsync(int userId, int productId, int quantity, CreateOrderDto dto)
        {
            await _cartRepo.AddOrReplaceSingleItemAsync(userId, productId, quantity);

            dto.UserId = userId;
            return await CreateOrderAsync(dto);
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus)
        {
            var order = await _orderRepo.GetByIdAsync(orderId);
            if (order == null) return false;

            // Validate status transitions
            var validStatuses = new[] { "Pending", "Paid", "Shipped", "Delivered", "Hoàn thành", "Cancelled" };
            if (!validStatuses.Contains(newStatus))
                throw new Exception("Trạng thái không hợp lệ");

            // If marking as Paid, update payment record as well (fake payment)
            if (newStatus == "Paid")
            {
                if (order.Payment == null)
                {
                    // create a payment if missing (defensive)
                    order.Payment = new Payment
                    {
                        PaymentMethod = "Unknown",
                        Amount = order.TotalAmount,
                        Status = "Paid",
                        PaidAt = DateTime.Now
                    };
                }
                else
                {
                    order.Payment.Status = "Paid";
                    order.Payment.PaidAt = DateTime.Now;
                }
            }

            order.Status = newStatus;
            await _orderRepo.UpdateAsync(order);
            return true;
        }

        //public async Task<bool> CancelOrderAsync(int orderId, int userId)
        //{
        //    var order = await _orderRepo.GetByIdAsync(orderId);
        //    if (order == null) return false;

        //    // Check ownership
        //    if (order.UserId != userId)
        //        throw new Exception("Bạn không có quyền hủy đơn hàng này");

        //    // Only allow cancellation if order is Pending
        //    if (order.Status != "Pending")
        //        throw new Exception("Chỉ có thể hủy đơn hàng ở trạng thái Pending");

        //    order.Status = "Cancelled";
        //    await _orderRepo.UpdateAsync(order);
        //    return true;
        //}

        public async Task<bool> CancelOrderAsync(int orderId, int userId)
        {
            // Lấy đầy đủ chi tiết để kiểm tra Shipping/Payment
            var order = await _orderRepo.GetByIdAsync(orderId, includeDetails: true);
            if (order == null || order.UserId != userId) return false;

            var isPending = order.Status == "Pending";
            var isPaid    = order.Payment?.Status == "Paid";
            var notShipped = order.Shipping == null || !order.Shipping.ShippedDate.HasValue;

            // Chỉ cho hủy nếu:
            // - Đơn đang Pending
            // - HOẶC đã thanh toán (Paid) nhưng CHƯA giao hàng
            if (!isPending && !(isPaid && notShipped))
                throw new Exception("Đơn hàng đã được xử lý, không thể hủy.");

            var wasPaid = isPaid;

            order.Status = "Cancelled"; // Hardcode string

            // Nếu đã thanh toán → hoàn kho và đánh dấu payment đã hoàn tiền
            if (wasPaid)
            {
                await _inventoryService.RestoreInventoryAsync(order);
                if (order.Payment != null)
                {
                    order.Payment.Status = "Refunded"; // ghi nhận đã hoàn tiền
                }
            }

            await _orderRepo.UpdateAsync(order);
            return true;
        }

        // ─── Shipper Flow ──────────────────────────────────────────────────────

        public async Task<bool> AssignShipperAsync(int orderId, int shipperId, string? trackingNumber, string? carrier)
        {
            var order = await _orderRepo.GetByIdAsync(orderId, includeDetails: true);
            if (order == null || order.Status != "Paid") return false;

            if (order.Shipping == null) return false;

            order.Shipping.ShipperId = shipperId;
            order.Shipping.TrackingNumber = trackingNumber;
            order.Shipping.Carrier = carrier;
            order.Shipping.ShippedDate = DateTime.Now;
            order.Status = "Shipped";

            await _orderRepo.UpdateAsync(order);
            return true;
        }

        public async Task<bool> MarkDeliveredAsync(int orderId, int shipperId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId, includeDetails: true);
            if (order == null || order.Status != "Shipped") return false;

            // Kiểm tra đúng shipper phụ trách
            if (order.Shipping == null || order.Shipping.ShipperId != shipperId) return false;

            order.Shipping.DeliveryDate = DateTime.Now;
            order.Status = "Delivered";

            await _orderRepo.UpdateAsync(order);
            return true;
        }

        public async Task<List<OrderDto>> GetShipperOrdersAsync(int shipperId)
        {
            var orders = await _orderRepo.GetByShipperIdAsync(shipperId);
            return orders.Select(MapToOrderDto).ToList();
        }

        public async Task<OrderDto?> GetOrderByIdForAdminAsync(int orderId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId, includeDetails: true);
            return order == null ? null : MapToOrderDto(order);
        }

        /// <summary>Khách hàng xác nhận đã nhận hàng → cập nhật trạng thái Hoàn thành</summary>
        public async Task<bool> ConfirmReceivedByCustomerAsync(int orderId, int userId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId, includeDetails: true);
            if (order == null || order.UserId != userId || order.Status != "Delivered") return false;

            order.Status = "Hoàn thành";
            await _orderRepo.UpdateAsync(order);
            return true;
        }

        /// <summary>Khách hàng báo chưa nhận hàng → hoàn tiền + cảnh báo shipper</summary>
        public async Task<bool> ReportNotReceivedByCustomerAsync(int orderId, int userId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId, includeDetails: true);
            if (order == null || order.UserId != userId || order.Status != "Delivered") return false;

            // Hoàn kho
            await _inventoryService.RestoreInventoryAsync(order);

            // Đánh dấu đã hoàn tiền
            if (order.Payment != null)
                order.Payment.Status = "Refunded";

            // Cảnh báo shipper
            if (order.Shipping != null)
            {
                order.Shipping.IsDisputed = true;
                order.Shipping.DisputeReportedAt = DateTime.UtcNow;
            }

            order.Status = "Cancelled";
            await _orderRepo.UpdateAsync(order);
            return true;
        }

        // Helper Map
        private OrderDto MapToOrderDto(Order order)
        {
            return new OrderDto
            {
                OrderId = order.OrderId,
                UserId = order.UserId,
                FullName = order.User?.FullName ?? "",
                UserName = order.User?.UserName ?? "",
                OrderDate = order.OrderDate,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                Note = order.Note,
                OrderItems = order.OrderItems.Select(oi => new OrderItemDto
                {
                    OrderItemId = oi.OrderItemId,
                    ProductId = oi.ProductId,
                    ProductName = oi.Product?.ProductName ?? "",
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                }).ToList(),
                Payment = order.Payment != null ? new PaymentDto
                {
                    PaymentId = order.Payment.PaymentId,
                    OrderId = order.OrderId,
                    PaymentMethod = order.Payment.PaymentMethod,
                    Amount = order.Payment.Amount,
                    PaidAt = order.Payment.PaidAt,
                    Status = order.Payment.Status
                } : null,
                Shipping = order.Shipping != null ? new ShippingDto
                {
                    ShippingId = order.Shipping.ShippingId,
                    OrderId = order.OrderId,
                    Address = order.Shipping.Address,
                    City = order.Shipping.City,
                    Country = order.Shipping.Country,
                    PostalCode = order.Shipping.PostalCode,
                    Carrier = order.Shipping.Carrier,
                    TrackingNumber = order.Shipping.TrackingNumber,
                    ShipperId = order.Shipping.ShipperId,
                    ShipperName = order.Shipping.Shipper?.FullName ?? order.Shipping.Shipper?.UserName,
                    ShippedDate = order.Shipping.ShippedDate,
                    DeliveryDate = order.Shipping.DeliveryDate,
                    IsDisputed = order.Shipping.IsDisputed,
                    DisputeReportedAt = order.Shipping.DisputeReportedAt
                } : null
            };
        }
    }
}
