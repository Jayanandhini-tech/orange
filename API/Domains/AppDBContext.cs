using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CMS.API.Domains;

public class AppDBContext : IdentityDbContext<IdentityUser>
{
    public AppDBContext(DbContextOptions<AppDBContext> options) : base(options)
    {
    }


    public DbSet<ApplicationUser> ApplicationUsers { get; set; }
    public DbSet<AppUser> AppUsers { get; set; }
    public DbSet<Company> Companies { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<CashRefund> CashRefunds { get; set; }
    public DbSet<EmailAddress> EmailAddresses { get; set; }
    public DbSet<FaceDeviceSetting> FaceDeviceSettings { get; set; }
    public DbSet<LogRequest> LogRequests { get; set; }
    public DbSet<Machine> Machines { get; set; }
    public DbSet<MachineUpdateInfo> MachineUpdateInfos { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<PgSetting> PgSettings { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<PaymentSetting> PaymentSettings { get; set; }
    public DbSet<PaymentAccountSetting> PaymentAccountSettings { get; set; }
    public DbSet<Recharge> Recharges { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Refund> Refunds { get; set; }
    public DbSet<ReportRequest> ReportRequests { get; set; }
    public DbSet<StockCleared> StockCleared { get; set; }
    public DbSet<StockMachine> StockMachines { get; set; }
    public DbSet<StockRefill> StockRefills { get; set; }
    public DbSet<Vendor> Vendors { get; set; }
}
