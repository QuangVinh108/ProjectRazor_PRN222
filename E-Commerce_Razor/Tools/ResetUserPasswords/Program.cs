using DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ResetUserPasswords;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Cập nhật PasswordHash cho tất cả Users ===\n");

        // Mật khẩu chung (có thể truyền qua tham số: dotnet run -- 123456)
        string commonPassword = args.Length > 0 ? args[0] : "123456";

        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .Build();

        var connectionString = config["ConnectionStrings:DefaultConnection"];
        if (string.IsNullOrEmpty(connectionString))
        {
            Console.WriteLine("Lỗi: Không tìm thấy ConnectionStrings:DefaultConnection trong appsettings.json");
            return;
        }

        var options = new DbContextOptionsBuilder<ShopDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        await using var db = new ShopDbContext(options);

        try
        {
            var users = await db.Users.ToListAsync();
            var usersToUpdate = users.Where(u => u.LoginProvider != "Google").ToList();
            var googleUsers = users.Count - usersToUpdate.Count;

            if (usersToUpdate.Count == 0)
            {
                Console.WriteLine("Không có user nào cần cập nhật (hoặc tất cả đều dùng Google login).");
                return;
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(commonPassword);

            foreach (var user in usersToUpdate)
            {
                user.PasswordHash = passwordHash;
            }

            await db.SaveChangesAsync();

            Console.WriteLine($"Đã cập nhật {usersToUpdate.Count} user với mật khẩu chung: {commonPassword}");
            if (googleUsers > 0)
                Console.WriteLine($"(Bỏ qua {googleUsers} user đăng nhập Google - không có PasswordHash)");
            Console.WriteLine("\nCó thể đăng nhập với bất kỳ tài khoản nào bằng mật khẩu trên.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Lỗi: {ex.Message}");
        }
    }
}
