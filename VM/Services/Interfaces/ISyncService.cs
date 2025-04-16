namespace VM.Services.Interfaces;

public interface ISyncService
{
    Task<bool> CheckInternetStatus();
    Task GetCompany();
    Task GetCategories();
    Task GetMachineInfo();
    Task GetProducts(bool recent = true);
    Task InitMotorSettings();
    Task LoadMachineData();
    Task OrderUpdateAsync();
    Task StockUpdateAsync();
    Task PushStockClearedAsync();
    Task PushStockRefillAsync();
    Task CashRefundUpdateAsync();
    Task AppUserUpdateAsync();
    Task SetDefaultPaymentSettings(string machineId = "");
    Task GetPaymentSettings();
}