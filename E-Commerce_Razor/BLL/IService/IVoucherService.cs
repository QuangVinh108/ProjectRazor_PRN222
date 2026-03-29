using BLL.DTOs;

namespace BLL.IService;

public interface IVoucherService
{
    /// <summary>Lấy tất cả voucher (dành cho Admin)</summary>
    Task<List<VoucherDto>> GetAllAsync();

    /// <summary>Lấy voucher theo Id</summary>
    Task<VoucherDto?> GetByIdAsync(int id);

    /// <summary>Tạo voucher mới (Admin)</summary>
    Task<VoucherDto> CreateAsync(CreateVoucherDto dto);

    /// <summary>Cập nhật voucher (Admin)</summary>
    Task UpdateAsync(int id, CreateVoucherDto dto);

    /// <summary>Xóa voucher (Admin)</summary>
    Task DeleteAsync(int id);

    /// <summary>
    /// Kiểm tra và áp dụng mã voucher cho một đơn hàng.
    /// Trả về số tiền được giảm và tổng tiền sau giảm.
    /// </summary>
    Task<ApplyVoucherResult> ApplyVoucherAsync(int userId, string code, decimal orderTotal);

    /// <summary>
    /// Lấy danh sách voucher hợp lệ ĐÃ LƯU TRONG VÍ cho một đơn hàng.
    /// </summary>
    Task<List<VoucherDto>> GetApplicableVouchersAsync(int userId, decimal orderTotal);

    // ─── USER VOUCHER WALLET ───────────────────────────────────────────────────
    Task SaveVoucherAsync(int userId, int voucherId);
    Task<List<int>> GetAllSavedVoucherIdsAsync(int userId);
    Task<List<VoucherDto>> GetSavedVouchersAsync(int userId);
    Task<bool> HasUserSavedVoucherAsync(int userId, int voucherId);
}
