using DAL.Entities;

namespace DAL.IRepository;

public interface IVoucherRepository
{
    /// <summary>Lấy tất cả Voucher (Admin)</summary>
    Task<List<Voucher>> GetAllAsync();

    /// <summary>Lấy voucher theo Id</summary>
    Task<Voucher?> GetByIdAsync(int id);

    /// <summary>Lấy voucher theo Code (không phân biệt hoa thường)</summary>
    Task<Voucher?> GetByCodeAsync(string code);

    /// <summary>Tạo voucher mới</summary>
    Task<Voucher> CreateAsync(Voucher voucher);

    /// <summary>Cập nhật voucher</summary>
    Task UpdateAsync(Voucher voucher);

    /// <summary>Xóa voucher</summary>
    Task DeleteAsync(int id);

    // ─── USER VOUCHER WALLET ───────────────────────────────────────────────────
    Task<bool> HasUserSavedVoucherAsync(int userId, int voucherId);
    Task<List<int>> GetAllSavedVoucherIdsAsync(int userId);
    Task SaveVoucherForUserAsync(int userId, int voucherId);
    Task<List<Voucher>> GetSavedVouchersByUserIdAsync(int userId);
    Task<UserVoucher?> GetUserVoucherAsync(int userId, int voucherId);
    Task UpdateUserVoucherAsync(UserVoucher userVoucher);
}
