using CMS.API.Domains;
using CMS.API.Repository.IRepository;
using CMS.API.Services.Interfaces;

namespace CMS.API.Repository;

public class UnitOfWork : IUnitOfWork
{

    private readonly AppDBContext dBContext;
    private readonly ITenant tenant;

    public IApplicationUserRepository ApplicationUsers { get; private set; }
    public IAppUserRepository AppUsers { get; private set; }
    public ICompanyRepository Companies { get; private set; }
    public ICategoryRepository Categories { get; private set; }
    public ICashRefundRepository CashRefunds { get; private set; }
    public IEmailAddressRepository EmailAddresses { get; private set; }
    public IMachineRepository Machines { get; private set; }
    public IMachineUpdateRepository MachineUpdates { get; private set; }
    public IProductRepository Products { get; private set; }
    public IRefreshTokenRepository RefreshTokens { get; private set; }
    public IStockClearedRepository StockCleared { get; private set; }
    public IStockMachineRepository StockMachine { get; private set; }
    public IStockRefillRepository StockRefills { get; private set; }
    public IVendorRepository Vendors { get; private set; }
    public IOrderRepository Orders { get; private set; }
    public IPaymentRepository Payments { get; private set; }
    public IOrderItemRepository OrderItems { get; private set; }
    public IRefundRepository Refunds { get; private set; }
    public IRechargeRepository Recharges { get; private set; }
    public IPgSettingRepository PgSettings { get; private set; }
    public IReportRequestRepository ReportRequests { get; private set; }
    public ILogRequestRepository LogRequests { get; private set; }
    public IPaymentSettingRepository PaymentSettings { get; private set; }
    public IPaymentAccountSettingRepository PaymentAccountSettings { get; private set; }
    public IFaceDeviceSettingRepository FaceDeviceSettings { get; private set; }

    public UnitOfWork(AppDBContext dBContext, ITenant tenant)
    {
        this.dBContext = dBContext;
        this.tenant = tenant;
        RefreshTokens = new RefreshTokenRepository(dBContext, tenant);
        ApplicationUsers = new ApplicationUserRepository(dBContext, tenant);
        AppUsers = new AppUserRepository(dBContext, tenant);
        Vendors = new VendorRepository(dBContext, tenant);
        Machines = new MachineRepository(dBContext, tenant);
        Products = new ProductRepository(dBContext, tenant);
        StockMachine = new StockMachineRepository(dBContext, tenant);
        MachineUpdates = new MachineUpdateRepository(dBContext, tenant);
        Categories = new CategoryRepository(dBContext, tenant);
        Orders = new OrderRepository(dBContext, tenant);
        Payments = new PaymentRepository(dBContext, tenant);
        OrderItems = new OrderItemRepository(dBContext, tenant);
        Refunds = new RefundRepository(dBContext, tenant);
        Recharges = new RechargeRepository(dBContext, tenant);
        PgSettings = new PgSettingRepository(dBContext, tenant);
        Companies = new CompanyRepository(dBContext, tenant);
        CashRefunds = new CashRefundRepository(dBContext, tenant);
        EmailAddresses = new EmailAddressRepository(dBContext, tenant);
        StockCleared = new StockClearedRepository(dBContext, tenant);
        StockRefills = new StockRefillRepository(dBContext, tenant);
        ReportRequests = new ReportRequestRepository(dBContext, tenant);
        LogRequests = new LogRequestRepository(dBContext, tenant);
        PaymentSettings = new PaymentSettingRepository(dBContext, tenant);
        PaymentAccountSettings = new PaymentAccountSettingRepository(dBContext, tenant);
        FaceDeviceSettings = new FaceDeviceSettingRepository(dBContext, tenant);
    }


    public async Task<int> SaveChangesAsync()
    {
        return await dBContext.SaveChangesAsync();
    }

}
