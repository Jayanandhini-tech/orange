using Microsoft.EntityFrameworkCore;

namespace VM.Domains;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<Company> Company => Set<Company>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Machine> Machines => Set<Machine>();
    public DbSet<MotorSetting> MotorSettings => Set<MotorSetting>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Refill> Refills => Set<Refill>();
    public DbSet<StockCleared> StockCleared => Set<StockCleared>();
    public DbSet<CashRefund> CashRefunds => Set<CashRefund>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<PaymentSetting> PaymentSettings => Set<PaymentSetting>();
    public DbSet<PaymentAccountSetting> PaymentAccountSettings => Set<PaymentAccountSetting>();
    public DbSet<FaceDeviceSetting> FaceDeviceSettings => Set<FaceDeviceSetting>();

    internal object Product(Func<object, object> value)
    {
        throw new NotImplementedException();
    }
}
