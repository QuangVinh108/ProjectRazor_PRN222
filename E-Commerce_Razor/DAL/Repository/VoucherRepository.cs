using DAL.Entities;
using DAL.IRepository;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repository;

public class VoucherRepository : IVoucherRepository
{
    private readonly ShopDbContext _context;

    public VoucherRepository(ShopDbContext context)
    {
        _context = context;
    }

    public async Task<List<Voucher>> GetAllAsync()
        => await _context.Vouchers.OrderByDescending(v => v.CreatedAt).ToListAsync();

    public async Task<Voucher?> GetByIdAsync(int id)
        => await _context.Vouchers.FindAsync(id);

    public async Task<Voucher?> GetByCodeAsync(string code)
        => await _context.Vouchers
            .FirstOrDefaultAsync(v => v.Code.ToUpper() == code.ToUpper().Trim());

    public async Task<Voucher> CreateAsync(Voucher voucher)
    {
        _context.Vouchers.Add(voucher);
        await _context.SaveChangesAsync();
        return voucher;
    }

    public async Task UpdateAsync(Voucher voucher)
    {
        _context.Vouchers.Update(voucher);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var voucher = await _context.Vouchers.FindAsync(id);
        if (voucher != null)
        {
            _context.Vouchers.Remove(voucher);
            await _context.SaveChangesAsync();
        }
    }

    // ─── USER VOUCHER WALLET ───────────────────────────────────────────────────

    public async Task<bool> HasUserSavedVoucherAsync(int userId, int voucherId)
    {
        return await _context.UserVouchers.AnyAsync(uv => uv.UserId == userId && uv.VoucherId == voucherId);
    }

    public async Task<List<int>> GetAllSavedVoucherIdsAsync(int userId)
    {
        return await _context.UserVouchers
            .Where(uv => uv.UserId == userId)
            .Select(uv => uv.VoucherId)
            .ToListAsync();
    }

    public async Task SaveVoucherForUserAsync(int userId, int voucherId)
    {
        if (!await HasUserSavedVoucherAsync(userId, voucherId))
        {
            _context.UserVouchers.Add(new UserVoucher
            {
                UserId = userId,
                VoucherId = voucherId,
                IsUsed = false,
                SavedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<Voucher>> GetSavedVouchersByUserIdAsync(int userId)
    {
        return await _context.UserVouchers
            .Where(uv => uv.UserId == userId && !uv.IsUsed)
            .OrderByDescending(uv => uv.SavedAt)
            .Select(uv => uv.Voucher)
            .ToListAsync();
    }

    public async Task<UserVoucher?> GetUserVoucherAsync(int userId, int voucherId)
    {
        return await _context.UserVouchers
            .FirstOrDefaultAsync(uv => uv.UserId == userId && uv.VoucherId == voucherId);
    }

    public async Task UpdateUserVoucherAsync(UserVoucher userVoucher)
    {
        _context.UserVouchers.Update(userVoucher);
        await _context.SaveChangesAsync();
    }
}
