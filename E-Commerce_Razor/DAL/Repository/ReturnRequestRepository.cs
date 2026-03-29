using DAL.Entities;
using DAL.IRepository;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repository
{
    public class ReturnRequestRepository : IReturnRequestRepository
    {
        private readonly ShopDbContext _context;

        public ReturnRequestRepository(ShopDbContext context)
        {
            _context = context;
        }

        public async Task<ReturnRequest?> GetByIdAsync(int id)
        {
            return await _context.ReturnRequests
                .Include(r => r.Order)
                    .ThenInclude(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                .Include(r => r.Order)
                    .ThenInclude(o => o.Payment)
                .Include(r => r.Order)
                    .ThenInclude(o => o.Shipping)
                .Include(r => r.User)
                .Include(r => r.ProcessedByUser)
                .FirstOrDefaultAsync(r => r.ReturnRequestId == id);
        }

        public async Task<ReturnRequest?> GetByOrderIdAsync(int orderId)
        {
            return await _context.ReturnRequests
                .Include(r => r.Order)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.OrderId == orderId);
        }

        public async Task<List<ReturnRequest>> GetByUserIdAsync(int userId)
        {
            return await _context.ReturnRequests
                .Include(r => r.Order)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<ReturnRequest>> GetAllPendingAsync()
        {
            return await _context.ReturnRequests
                .Include(r => r.Order)
                    .ThenInclude(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                .Include(r => r.Order)
                    .ThenInclude(o => o.Payment)
                .Include(r => r.User)
                .Where(r => r.Status == "Pending")
                .OrderBy(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<ReturnRequest>> GetAllAsync()
        {
            return await _context.ReturnRequests
                .Include(r => r.Order)
                    .ThenInclude(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                .Include(r => r.Order)
                    .ThenInclude(o => o.Payment)
                .Include(r => r.User)
                .Include(r => r.ProcessedByUser)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<ReturnRequest> CreateAsync(ReturnRequest entity)
        {
            _context.ReturnRequests.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task UpdateAsync(ReturnRequest entity)
        {
            _context.ReturnRequests.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsByOrderIdAsync(int orderId)
        {
            return await _context.ReturnRequests
                .AnyAsync(r => r.OrderId == orderId && r.Status != "Rejected");
        }
    }
}
