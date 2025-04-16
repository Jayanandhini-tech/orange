namespace CMS.API.Repository.IRepository;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync();
    IApplicationUserRepository ApplicationUsers { get; }
    IAppUserRepository AppUsers { get; }
    ICategoryRepository Categories { get; }
    ICompanyRepository Companies { get; }
    ICashRefundRepository CashRefunds { get; }
    IEmailAddressRepository EmailAddresses { get; }
    ILogRequestRepository LogRequests { get; }
    IMachineRepository Machines { get; }
    IMachineUpdateRepository MachineUpdates { get; }
    IProductRepository Products { get; }
    IRefreshTokenRepository RefreshTokens { get; }
    IStockClearedRepository StockCleared { get; }
    IStockMachineRepository StockMachine { get; }
    IStockRefillRepository StockRefills { get; }
    IVendorRepository Vendors { get; }
    IOrderRepository Orders { get; }
    IOrderItemRepository OrderItems { get; }
    IPaymentRepository Payments { get; }
    IRefundRepository Refunds { get; }
    IRechargeRepository Recharges { get; }
    IPgSettingRepository PgSettings { get; }
    IReportRequestRepository ReportRequests { get; }
    IPaymentSettingRepository PaymentSettings { get; }
    IPaymentAccountSettingRepository PaymentAccountSettings { get; }
    IFaceDeviceSettingRepository FaceDeviceSettings { get; }
}
