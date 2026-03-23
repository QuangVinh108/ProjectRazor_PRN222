using BLL.DTOs;
using BLL.IService;
using DAL.Entities;
using DAL.IRepository;

namespace BLL.Service;

public class VoucherService : IVoucherService
{
    private readonly IVoucherRepository _voucherRepo;

    public VoucherService(IVoucherRepository voucherRepo)
    {
        _voucherRepo = voucherRepo;
    }

    public async Task<List<VoucherDto>> GetAllAsync()
    {
        var vouchers = await _voucherRepo.GetAllAsync();
        return vouchers.Select(MapToDto).ToList();
    }

    public async Task<VoucherDto?> GetByIdAsync(int id)
    {
        var v = await _voucherRepo.GetByIdAsync(id);
        return v == null ? null : MapToDto(v);
    }

    public async Task<VoucherDto> CreateAsync(CreateVoucherDto dto)
    {
        // Validate ngày
        if (dto.EndDate <= dto.StartDate)
            throw new Exception("Ngày kết thúc phải sau ngày bắt đầu.");

        // Validate Percent không vượt 100
        if (dto.DiscountType == "Percent" && dto.DiscountValue > 100)
            throw new Exception("Phần trăm giảm giá không được vượt quá 100%.");

        // Kiểm tra code đã tồn tại chưa
        var existing = await _voucherRepo.GetByCodeAsync(dto.Code);
        if (existing != null)
            throw new Exception($"Mã voucher '{dto.Code}' đã tồn tại.");

        var voucher = new Voucher
        {
            Code         = dto.Code.ToUpper().Trim(),
            Description  = dto.Description,
            DiscountType = dto.DiscountType,
            DiscountValue= dto.DiscountValue,
            MinOrderValue= dto.MinOrderValue,
            MaxDiscount  = dto.MaxDiscount,
            UsageLimit   = dto.UsageLimit,
            UsedCount    = 0,
            StartDate    = dto.StartDate,
            EndDate      = dto.EndDate,
            IsActive     = dto.IsActive
        };

        var created = await _voucherRepo.CreateAsync(voucher);
        return MapToDto(created);
    }

    public async Task UpdateAsync(int id, CreateVoucherDto dto)
    {
        var voucher = await _voucherRepo.GetByIdAsync(id)
            ?? throw new Exception("Không tìm thấy voucher.");

        if (dto.EndDate <= dto.StartDate)
            throw new Exception("Ngày kết thúc phải sau ngày bắt đầu.");

        if (dto.DiscountType == "Percent" && dto.DiscountValue > 100)
            throw new Exception("Phần trăm giảm giá không được vượt quá 100%.");

        // Kiểm tra code trùng với voucher khác
        var existing = await _voucherRepo.GetByCodeAsync(dto.Code);
        if (existing != null && existing.VoucherId != id)
            throw new Exception($"Mã voucher '{dto.Code}' đã tồn tại.");

        voucher.Code         = dto.Code.ToUpper().Trim();
        voucher.Description  = dto.Description;
        voucher.DiscountType = dto.DiscountType;
        voucher.DiscountValue= dto.DiscountValue;
        voucher.MinOrderValue= dto.MinOrderValue;
        voucher.MaxDiscount  = dto.MaxDiscount;
        voucher.UsageLimit   = dto.UsageLimit;
        voucher.StartDate    = dto.StartDate;
        voucher.EndDate      = dto.EndDate;
        voucher.IsActive     = dto.IsActive;

        await _voucherRepo.UpdateAsync(voucher);
    }

    public async Task DeleteAsync(int id)
    {
        var voucher = await _voucherRepo.GetByIdAsync(id)
            ?? throw new Exception("Không tìm thấy voucher.");
        await _voucherRepo.DeleteAsync(id);
    }

    /// <summary>
    /// Kiểm tra tính hợp lệ và tính số tiền được giảm (dựa trên Ví Voucher của User).
    /// </summary>
    public async Task<ApplyVoucherResult> ApplyVoucherAsync(int userId, string code, decimal orderTotal)
    {
        var voucher = await _voucherRepo.GetByCodeAsync(code);

        if (voucher == null)
            return Fail("Mã giảm giá không tồn tại.");

        var userVoucher = await _voucherRepo.GetUserVoucherAsync(userId, voucher.VoucherId);
        if (userVoucher == null)
            return Fail("Bạn chưa lưu mã giảm giá này vào ví.");
        
        if (userVoucher.IsUsed)
            return Fail("Bạn đã sử dụng mã giảm giá này rồi.");

        if (!voucher.IsActive)
            return Fail("Mã giảm giá đã bị vô hiệu hóa.");

        var now = DateTime.Now;
        if (now < voucher.StartDate || now > voucher.EndDate)
            return Fail("Mã giảm giá đã hết hạn hoặc chưa đến ngày sử dụng.");

        if (voucher.UsedCount >= voucher.UsageLimit)
            return Fail("Mã giảm giá đã được sử dụng hết trên toàn hệ thống.");

        if (orderTotal < voucher.MinOrderValue)
            return Fail($"Đơn hàng tối thiểu phải từ {voucher.MinOrderValue:N0}đ để dùng mã này.");

        // Tính số tiền được giảm
        decimal discountAmount;
        if (voucher.DiscountType == "Percent")
        {
            discountAmount = orderTotal * (voucher.DiscountValue / 100m);
            if (voucher.MaxDiscount.HasValue)
                discountAmount = Math.Min(discountAmount, voucher.MaxDiscount.Value);
        }
        else // Fixed
        {
            discountAmount = voucher.DiscountValue;
        }

        // Không cho giảm quá tổng đơn
        discountAmount = Math.Min(discountAmount, orderTotal);

        return new ApplyVoucherResult
        {
            IsSuccess      = true,
            DiscountAmount = discountAmount,
            FinalAmount    = orderTotal - discountAmount,
            Voucher        = MapToDto(voucher)
        };
    }

    public async Task<List<VoucherDto>> GetApplicableVouchersAsync(int userId, decimal orderTotal)
    {
        var now = DateTime.Now;
        var savedVouchers = await _voucherRepo.GetSavedVouchersByUserIdAsync(userId);
        
        var applicable = savedVouchers.Where(v => 
            v.IsActive && 
            now >= v.StartDate && 
            now <= v.EndDate && 
            v.UsedCount < v.UsageLimit && 
            orderTotal >= v.MinOrderValue
        ).ToList();

        return applicable.Select(MapToDto).ToList();
    }

    // ─── USER VOUCHER WALLET ───────────────────────────────────────────────────

    public async Task SaveVoucherAsync(int userId, int voucherId)
    {
        // 1. Kiểm tra voucher tồn tại
        var voucher = await _voucherRepo.GetByIdAsync(voucherId);
        if (voucher == null || !voucher.IsActive || DateTime.Now > voucher.EndDate)
            throw new Exception("Mã giảm giá không tồn tại hoặc đã hết hạn.");

        // 2. Kiểm tra xem user đã lưu chưa
        var hasSaved = await _voucherRepo.HasUserSavedVoucherAsync(userId, voucherId);
        if (hasSaved)
            throw new Exception("Bạn đã lưu mã này rồi.");

        // 3. Kiểm tra số lượt còn lại (nếu quá đông người lưu)
        // (Optional: Không giới hạn lưu nhưng ai nhanh tay mua trước thì được)
        
        await _voucherRepo.SaveVoucherForUserAsync(userId, voucherId);
    }

    public async Task<List<int>> GetAllSavedVoucherIdsAsync(int userId)
    {
        return await _voucherRepo.GetAllSavedVoucherIdsAsync(userId);
    }

    public async Task<List<VoucherDto>> GetSavedVouchersAsync(int userId)
    {
        var userVouchers = await _voucherRepo.GetSavedVouchersByUserIdAsync(userId);
        return userVouchers.Select(MapToDto).ToList();
    }

    public async Task<bool> HasUserSavedVoucherAsync(int userId, int voucherId)
    {
        return await _voucherRepo.HasUserSavedVoucherAsync(userId, voucherId);
    }

    // ─── Helper ─────────────────────────────────────────────────────────────

    private static ApplyVoucherResult Fail(string message) => new()
    {
        IsSuccess    = false,
        ErrorMessage = message
    };

    private static VoucherDto MapToDto(Voucher v) => new()
    {
        VoucherId     = v.VoucherId,
        Code          = v.Code,
        Description   = v.Description,
        DiscountType  = v.DiscountType,
        DiscountValue = v.DiscountValue,
        MinOrderValue = v.MinOrderValue,
        MaxDiscount   = v.MaxDiscount,
        UsageLimit    = v.UsageLimit,
        UsedCount     = v.UsedCount,
        StartDate     = v.StartDate,
        EndDate       = v.EndDate,
        IsActive      = v.IsActive
    };
}
