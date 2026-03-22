using DAL.Entities;

namespace DAL.IRepository
{
    public interface IReturnRequestRepository
    {
        Task<ReturnRequest?> GetByIdAsync(int id);
        Task<ReturnRequest?> GetByOrderIdAsync(int orderId);
        Task<List<ReturnRequest>> GetByUserIdAsync(int userId);
        Task<List<ReturnRequest>> GetAllPendingAsync();
        Task<List<ReturnRequest>> GetAllAsync();
        Task<ReturnRequest> CreateAsync(ReturnRequest entity);
        Task UpdateAsync(ReturnRequest entity);
        Task<bool> ExistsByOrderIdAsync(int orderId);
    }
}
