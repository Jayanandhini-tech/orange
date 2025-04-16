using CMS.Dto;
using CMS.Dto.Products;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Net;
using System.Net.Http.Json;
using VM.Domains;
using VM.Dtos;
using VM.Services.Interfaces;

namespace VM.Services;

public class SyncService : ISyncService
{
    private readonly IServerClient httpClient;
    private readonly AppDbContext dbContext;
    private readonly IConfiguration configuration;
    private readonly ILogger<SyncService> logger;

    public SyncService(IServerClient serverClient, AppDbContext dbContext, IConfiguration configuration, ILogger<SyncService> logger)
    {
        this.httpClient = serverClient;
        this.dbContext = dbContext;
        this.configuration = configuration;
        this.logger = logger;
    }


    public async Task<bool> CheckInternetStatus()
    {
        try
        {
            return await httpClient.IsServerConnectable();
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            return false;
        }
    }

    public async Task GetMachineInfo()
    {
        try
        {
            var response = await httpClient.GetAsync("/api/machines/myinfo");

            if (response.StatusCode != HttpStatusCode.OK)
            {
                string respJson = await response.Content.ReadAsStringAsync();
                logger.LogInformation($"Status Code : {response.StatusCode}, Response :{respJson}");
                return;
            }

            var machineInfo = await response.Content.ReadFromJsonAsync<MachineInfoDto>();

            if (machineInfo is null)
                return;

            var machineId = configuration["MachineKey"]!;

            var machine = await dbContext.Machines.FindAsync(machineId);
            if (machine is null)
            {
                await dbContext.Machines.AddAsync(new Machine()
                {
                    Id = machineId,
                    MachineNumber = machineInfo.MachineNumber,
                    Name = machineInfo.Name,
                    AppType = machineInfo.AppType,
                    VendorShortName = machineInfo.VendorShortName,
                    Location = machineInfo.Location,
                });
            }
            else
            {
                machine.Location = machineInfo.Location;
                machine.Name = machineInfo.Name;
                machine.AppType = machineInfo.AppType;
                machine.VendorShortName = machineInfo.VendorShortName;
                machine.MachineNumber = machineInfo.MachineNumber;
            }

            await dbContext.SaveChangesAsync();

            return;
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            return;
        }
    }

    public async Task GetPaymentSettings()
    {
        try
        {
            string machineId = configuration["MachineKey"]!;

            var response = await httpClient.GetAsync("/api/machines/paymentsettings");

            if (response.StatusCode != HttpStatusCode.OK)
            {
                string respJson = await response.Content.ReadAsStringAsync();
                logger.LogInformation($"Status Code : {response.StatusCode}, Response :{respJson}");
                return;
            }


            var serverSettingsDto = await response.Content.ReadFromJsonAsync<PaymentSettingsDto>();

            if (serverSettingsDto is null)
                return;

            var paymentSetting = await dbContext.PaymentSettings.FindAsync(machineId);


            if (paymentSetting is not null)
            {
                paymentSetting.Upi = serverSettingsDto.Upi;
                paymentSetting.Cash = serverSettingsDto.Cash;
                paymentSetting.Account = serverSettingsDto.Account;
                paymentSetting.Card = serverSettingsDto.Card;
                paymentSetting.Counter = serverSettingsDto.Counter;

                if (serverSettingsDto.Account)
                {
                    var accSettings = await dbContext.PaymentAccountSettings.FindAsync(machineId);
                    if (accSettings is not null)
                    {
                        accSettings.AccountPlan = serverSettingsDto.AccountSettings.AccountPlan;
                        accSettings.AuthType = serverSettingsDto.AccountSettings.AuthType;
                        accSettings.DailyLimit = serverSettingsDto.AccountSettings.DailyLimit;
                        accSettings.MonthlyLimit = serverSettingsDto.AccountSettings.MonthlyLimit;
                    }

                    if (serverSettingsDto.AccountSettings.AuthType == StrDir.AuthPage.FACE)
                    {
                        var faceSetting = await dbContext.FaceDeviceSettings.FindAsync(machineId);
                        if (faceSetting is not null)
                        {
                            faceSetting.IpAddress = serverSettingsDto.FaceDeviceSettings.IpAddress;
                        }
                    }
                }
            }

            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    public async Task SetDefaultPaymentSettings(string machineId = "")
    {
        try
        {
            if (string.IsNullOrEmpty(machineId))
                machineId = configuration["MachineKey"]!;


            bool exist = await dbContext.PaymentSettings.AnyAsync(x => x.MachineId == machineId);
            if (exist)
                return;

            var paymentSettings = new PaymentSetting()
            {
                MachineId = machineId,
                Upi = true,
                Cash = false,
                Account = false,
                Card = false,
                Counter = false
            };
            dbContext.PaymentSettings.Add(paymentSettings);

            var payAccSettings = new PaymentAccountSetting()
            {
                MachineId = machineId,
                AccountPlan = StrDir.AccountPlan.PREPAID,
                AuthType = StrDir.AuthPage.IDCARD,
                DailyLimit = 0,
                MonthlyLimit = 0
            };

            dbContext.PaymentAccountSettings.Add(payAccSettings);
            dbContext.FaceDeviceSettings.Add(new FaceDeviceSetting() { MachineId = machineId, IpAddress = "127.0.0.1" });

            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
    }

    public async Task LoadMachineData()
    {
        try
        {
            var machineId = configuration["MachineKey"]!;

            var machine = await dbContext.Machines.FindAsync(machineId);

            if (machine is not null)
            {
                DataStore.MachineInfo = machine;
                DataStore.AppType = machine.AppType;
                DataStore.CategoryWiseToken = configuration.GetValue<bool>("CategoryWiseToken");
                DataStore.PrinterName = configuration.GetValue<string>("PrinterName") ?? "TVS-E RP 3230";
                DataStore.CabinCount = configuration.GetValue<int>("CabinCount", 1);
                DataStore.AntiThieft = configuration.GetValue<bool>("AntiThieft");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    public async Task InitMotorSettings()
    {
        try
        {
            if (!dbContext.MotorSettings.Any())
            {
                for (int i = 1; i <= 10; i++)
                {
                    await dbContext.MotorSettings.AddAsync(
                        new MotorSetting()
                        {
                            Id = i,
                            CabinId = 1,
                            MotorNumber = i,
                            ProductId = string.Empty,
                            Capacity = 3,
                            Stock = 0,
                            SoldOut = false,
                            UpdatedOn = DateTime.Now,
                            IsViewed = false,
                            IsActive = true
                        });
                }

                await dbContext.SaveChangesAsync();
            }

            if (DataStore.CabinCount == 2 && !dbContext.MotorSettings.Any(x => x.CabinId == 2))
            {

                int lastId = await dbContext.MotorSettings.MaxAsync(x => x.Id);

                for (int i = 1; i <= 60; i++)
                {
                    lastId++;
                    await dbContext.MotorSettings.AddAsync(
                        new MotorSetting()
                        {
                            Id = lastId,
                            CabinId = 2,
                            MotorNumber = i,
                            ProductId = string.Empty,
                            Capacity = 3,
                            Stock = 0,
                            SoldOut = false,
                            UpdatedOn = DateTime.Now,
                            IsViewed = false,
                            IsActive = true
                        });
                }

                await dbContext.SaveChangesAsync();
            }

        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    public async Task GetCompany()
    {
        try
        {
            var response = await httpClient.GetAsync("/api/company");

            if (response.StatusCode != HttpStatusCode.OK)
            {
                string respJson = await response.Content.ReadAsStringAsync();
                logger.LogInformation($"Status Code : {response.StatusCode}, Response :{respJson}");
                return;
            }

            var companies = await response.Content.ReadFromJsonAsync<List<Domains.Company>>();

            if (companies is null)
                return;

            foreach (var company in companies)
            {
                var localCompany = await dbContext.Company.FindAsync(company.Id);
                if (localCompany is null)
                {
                    await dbContext.Company.AddAsync(
                        new Domains.Company()
                        {
                            Id = company.Id,
                            Name = company.Name,
                            Address = company.Address,
                            GstNumber = company.GstNumber,
                            Phone = company.Phone
                        });
                }
                else
                {
                    localCompany.Name = company.Name;
                    localCompany.Address = company.Address;
                    localCompany.GstNumber = company.GstNumber;
                    localCompany.Phone = company.Phone;
                }
            }

            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    public async Task GetCategories()
    {
        try
        {
            var response = await httpClient.GetAsync("/api/categories");

            if (response.StatusCode != HttpStatusCode.OK)
            {
                string respJson = await response.Content.ReadAsStringAsync();
                logger.LogInformation($"Status Code : {response.StatusCode}, Response :{respJson}");
                return;
            }

            var categories = await response.Content.ReadFromJsonAsync<List<CategoryDto>>();

            if (categories is null)
                return;

            foreach (var category in categories)
            {
                await httpClient.DownloadImage(category.ImgPath, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, category.ImgPath));

                var localCategory = await dbContext.Categories.FindAsync(category.Id);
                if (localCategory is null)
                {
                    await dbContext.Categories.AddAsync(
                        new Category()
                        {
                            Id = category.Id,
                            Name = category.Name,
                            DisplayOrder = category.DisplayOrder,
                            ImgPath = category.ImgPath,
                            UpdatedOn = category.UpdatedOn
                        });
                }
                else
                {
                    if (localCategory.UpdatedOn != category.UpdatedOn)
                    {
                        localCategory.Name = category.Name;
                        localCategory.ImgPath = category.ImgPath;
                        localCategory.DisplayOrder = category.DisplayOrder;
                        localCategory.UpdatedOn = category.UpdatedOn;
                    }
                }
            }

            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    public async Task GetProducts(bool recent = true)
    {
        try
        {
            string url = "";

            if (recent)
            {
                var lastupdated = await dbContext.Products.MaxAsync(x => (DateTime?)x.UpdatedOn) ?? new DateTime(2000 - 01 - 01);
                url = $"/api/products?lastUpdaetdOn={lastupdated.ToString("yyyy-MM-dd HH:mm:ss")}";
            }
            else
            {
                url = "/api/products";
            }

            var response = await httpClient.GetAsync(url);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                string respJson = await response.Content.ReadAsStringAsync();
                logger.LogInformation($"Status Code : {response.StatusCode}, Response :{respJson}");
                return;
            }

            var prodcuts = await response.Content.ReadFromJsonAsync<List<ProductDto>>();

            if (prodcuts is null)
                return;

            foreach (var product in prodcuts)
            {
                await httpClient.DownloadImage(product.ImgPath, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, product.ImgPath));

                var localProduct = await dbContext.Products.FindAsync(product.Id);
                if (localProduct is null)
                {
                    await dbContext.Products.AddAsync(
                        new Product()
                        {
                            Id = product.Id,
                            CategoryId = product.CategoryId,
                            Name = product.Name,
                            Price = product.Price,
                            BaseRate = product.BaseRate,
                            Gst = product.gst,
                            ImgPath = product.ImgPath,
                            UpdatedOn = product.UpdatedOn
                        });
                }
                else
                {
                    if (localProduct.UpdatedOn != product.UpdatedOn)
                    {
                        localProduct.CategoryId = product.CategoryId;
                        localProduct.Name = product.Name;
                        localProduct.Price = product.Price;
                        localProduct.BaseRate = product.BaseRate;
                        localProduct.Gst = product.gst;
                        localProduct.ImgPath = product.ImgPath;
                        localProduct.UpdatedOn = product.UpdatedOn;
                    }
                }
            }

            await dbContext.SaveChangesAsync();

            var serverProductIds = prodcuts.Select(x => x.Id).ToList();
            var oldProducts = dbContext.Products.Where(x => !serverProductIds.Contains(x.Id));
            dbContext.Products.RemoveRange(oldProducts);

            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    public async Task OrderUpdateAsync()
    {
        try
        {
            var orders = await dbContext.Orders.Include(x => x.Items).Where(x => x.IsViewed == false).ToListAsync();
            if (orders.Any())
            {
                orders = orders.OrderBy(x => x.OrderDate).ToList();
                int limit = 10;
                int loops = (orders.Count / 10) + 1;

                for (int i = 0; i < loops; i++)
                {
                    int skip = i * limit;
                    var selectedOrders = orders.Skip(skip).Take(limit).ToList();

                    List<OrderDto> ordersDto = selectedOrders.Adapt<List<OrderDto>>();
                    var response = await httpClient.PostAsync($"/api/order/sync", ordersDto);
                    if (response.IsSuccessStatusCode)
                    {
                        var successOrders = await response.Content.ReadFromJsonAsync<OrderSyncResultDto>();
                        foreach (string successOrder in successOrders?.OrderNumbers ?? [])
                        {
                            var order = orders.First(x => x.OrderNumber == successOrder);
                            order.IsViewed = true;
                        }
                        await dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        var respText = await response.Content.ReadAsStringAsync();
                        logger.LogInformation($"Issue with order sync. Response code : {response.StatusCode}. Response : {respText}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    public async Task StockUpdateAsync()
    {
        try
        {
            var stocks = await dbContext.MotorSettings
                                .Where(x => x.IsViewed == false)
                                .ToListAsync();
            if (stocks.Any())
            {
                var stockUpdateDto = stocks.Adapt<List<StockMachineDto>>();
                var response = await httpClient.PostAsync($"/api/vm/stock", stockUpdateDto);
                if (response.IsSuccessStatusCode)
                {
                    var resultDto = await response.Content.ReadFromJsonAsync<StockUpdateResultDto>();
                    foreach (int motorNumber in resultDto?.MotorNumbers ?? [])
                    {
                        var stock = stocks.First(x => x.MotorNumber == motorNumber);
                        stock.IsViewed = true;
                    }
                    await dbContext.SaveChangesAsync();
                }
                else
                {
                    var respText = await response.Content.ReadAsStringAsync();
                    logger.LogInformation($"Issue with stock sync. Response code : {response.StatusCode}. Response : {respText}");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    public async Task PushStockClearedAsync()
    {
        try
        {
            var stockCleared = await dbContext.StockCleared.Where(x => x.IsViewed == false).ToListAsync();
            if (stockCleared.Any())
            {
                int limit = 10;
                int loops = (stockCleared.Count / 10) + 1;

                for (int i = 0; i < loops; i++)
                {
                    int skip = i * limit;
                    var selectedItem = stockCleared.Skip(skip).Take(limit).ToList();


                    var clearedDto = selectedItem.Adapt<List<StockClearedDto>>();
                    var response = await httpClient.PostAsync($"/api/vm/stock/cleared", clearedDto);
                    if (response.IsSuccessStatusCode)
                    {
                        var updatedIds = await response.Content.ReadFromJsonAsync<List<string>>();
                        foreach (string id in updatedIds ?? [])
                        {
                            var stock = stockCleared.First(x => x.Id == id);
                            stock.IsViewed = true;
                        }
                        await dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        var respText = await response.Content.ReadAsStringAsync();
                        logger.LogInformation($"Issue with stock cleared sync. Response code : {response.StatusCode}. Response : {respText}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    public async Task PushStockRefillAsync()
    {
        try
        {
            var stockRefill = await dbContext.Refills.Where(x => x.IsViewed == false).ToListAsync();
            if (stockRefill.Any())
            {
                int limit = 10;
                int loops = (stockRefill.Count / 10) + 1;

                for (int i = 0; i < loops; i++)
                {
                    int skip = i * limit;
                    var selectedItem = stockRefill.Skip(skip).Take(limit).ToList();


                    var refillDto = stockRefill.Adapt<List<StockRefillDto>>();
                    var response = await httpClient.PostAsync($"/api/vm/stock/refill", refillDto);
                    if (response.IsSuccessStatusCode)
                    {
                        var updatedIds = await response.Content.ReadFromJsonAsync<List<string>>();
                        foreach (string id in updatedIds ?? [])
                        {
                            var stock = stockRefill.First(x => x.Id == id);
                            stock.IsViewed = true;
                        }
                        await dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        var respText = await response.Content.ReadAsStringAsync();
                        logger.LogInformation($"Issue with stock refill sync. Response code : {response.StatusCode}. Response : {respText}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    public async Task CashRefundUpdateAsync()
    {
        try
        {
            var cashRefunds = await dbContext.CashRefunds.Where(x => x.IsViewed == false).ToListAsync();
            if (cashRefunds.Any())
            {
                var refundsDto = cashRefunds.Adapt<List<CashRefundDto>>();
                var response = await httpClient.PostAsync("api/cashrefund/sync", refundsDto);
                if (response.IsSuccessStatusCode)
                {
                    var ids = await response.Content.ReadFromJsonAsync<List<string>>() ?? [];
                    if (ids.Any())
                    {
                        await dbContext.CashRefunds.Where(x => ids.Contains(x.RefId)).ExecuteUpdateAsync(c => c.SetProperty(b => b.IsViewed, true));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    public async Task AppUserUpdateAsync()
    {
        try
        {
            var users = await dbContext.AppUsers.Where(x => x.IsViewed == false).ToListAsync();
            if (users.Any())
            {
                List<AppUserCreateDto> appUsers = users.Select(x => new AppUserCreateDto(x.Id, x.Name, x.ImagePath)).ToList();
                var response = await httpClient.PostAsync("api/appuser/sync", appUsers);
                if (response.IsSuccessStatusCode)
                {
                    var ids = await response.Content.ReadFromJsonAsync<List<string>>() ?? [];
                    if (ids.Any())
                    {
                        await dbContext.AppUsers.Where(x => ids.Contains(x.Id)).ExecuteUpdateAsync(c => c.SetProperty(b => b.IsViewed, true));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }
}
