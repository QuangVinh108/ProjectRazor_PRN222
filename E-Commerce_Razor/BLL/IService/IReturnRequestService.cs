using BLL.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BLL.IService
{
    public interface IReturnRequestService
    {
        /// <summary>Customer tạo yêu cầu trả hàng/hoàn tiền</summary>
        Task<(bool Success, string Message)> CreateReturnRequestAsync(CreateReturnRequestDto dto);

        /// <summary>Admin duyệt/từ chối yêu cầu trả hàng</summary>
        Task<(bool Success, string Message)> ProcessReturnRequestAsync(ProcessReturnRequestDto dto);

        /// <summary>Lấy danh sách yêu cầu trả hàng của 1 user</summary>
        Task<List<ReturnRequestDto>> GetByUserIdAsync(int userId);

        /// <summary>Lấy tất cả yêu cầu đang chờ (cho Admin)</summary>
        Task<List<ReturnRequestDto>> GetAllPendingAsync();

        /// <summary>Lấy tất cả yêu cầu (cho Admin)</summary>
        Task<List<ReturnRequestDto>> GetAllAsync();

        /// <summary>Lấy chi tiết 1 yêu cầu</summary>
        Task<ReturnRequestDto?> GetByIdAsync(int id);

        /// <summary>Kiểm tra đơn hàng đã có yêu cầu trả hàng chưa</summary>
        Task<bool> HasReturnRequestAsync(int orderId);

        /// <summary>Lấy thông tin ReturnRequest thông qua OrderId</summary>
        Task<ReturnRequestDto?> GetByOrderIdAsync(int orderId);
    }
}
