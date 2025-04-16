using CMS.Dto;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using VM.Domains;
using VM.Dtos;
using VM.Services.Interfaces;

namespace VM.Services;

public class UserService : IUserService
{
    private readonly AppDbContext dbContext;
    private readonly IServerClient serverClient;
    private readonly PaymentConfig paymentConfig;
    private readonly ILogger<UserService> logger;

    public UserService(AppDbContext dbContext, IServerClient serverClient, PaymentConfig paymentConfig, ILogger<UserService> logger)
    {
        this.dbContext = dbContext;
        this.serverClient = serverClient;
        this.paymentConfig = paymentConfig;
        this.logger = logger;
    }


    public async Task<AppUser?> GetUserAsync(string userId)
    {
        try
        {
            var user = await dbContext.AppUsers.FirstOrDefaultAsync(x => x.Id == userId);
            return user;
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            return null;
        }
    }

    public async Task<AppUser> CreateUserAsync(string userId, string name, string imgPath = "")
    {
        var user = new AppUser { Id = userId, Name = name, CreditLimit = 2500, ImagePath = imgPath, CreatedOn = DateTime.Now, IsViewed = false, IsActive = true };
        try
        {
            await dbContext.AppUsers.AddAsync(user);
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
        return user;
    }

    public async Task PostNewUserAsync(string userId)
    {
        try
        {
            var user = await dbContext.AppUsers.FirstOrDefaultAsync(x => x.Id == userId);
            if (user is not null)
            {
                var response = await serverClient.PostAsync($"api/appuser", new AppUserCreateDto(user.Id, user.Name, user.ImagePath));
                if (response.IsSuccessStatusCode)
                {
                    var resp = await response.Content.ReadFromJsonAsync<AppUserRegisterResponse>() ?? new AppUserRegisterResponse(false, "");
                    if (resp.Success)
                    {
                        user.IsViewed = true;
                        await dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        logger.LogError($"Error in Create User on Server. User Id {user.Id}, Response : {resp.Message}");
                    }
                }
                else
                {
                    var respText = await response.Content.ReadAsStringAsync();
                    logger.LogError(respText);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    public async Task<double> GetBalanceAsync(string userId)
    {
        AppUserBalanceDto balanceDto = new AppUserBalanceDto(userId, 0);
        try
        {
            if (paymentConfig.AccountConfig.Plan == StrDir.AccountPlan.POSTPAID)
            {
                DateTime startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                double spend = await dbContext.Orders.Where(x => x.UserId == userId && x.OrderDate >= startDate && x.Status == StrDir.OrderStatus.SUCCESS).Select(x => x.Total).SumAsync();
                balanceDto = new AppUserBalanceDto(userId, spend);
            }

            if (paymentConfig.AccountConfig.Plan == StrDir.AccountPlan.PREPAID)
            {
                var response = await serverClient.GetAsync($"api/appuser/balance?id={userId}&plan={paymentConfig.AccountConfig.Plan}");
                if (response.IsSuccessStatusCode)
                    balanceDto = await response.Content.ReadFromJsonAsync<AppUserBalanceDto>() ?? new AppUserBalanceDto(userId, 0);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }

        return balanceDto.Balance;
    }

    public async Task<AppUserTransactionDto> GetTransactionsAsync(string userId)
    {
        try
        {
            if (paymentConfig.AccountConfig.Plan == StrDir.AccountPlan.POSTPAID)
            {
                var orders = await dbContext.Orders
                                        .Include(x => x.Items)
                                        .Where(x => x.UserId == userId)
                                        .Select(x => new AccountOrderTransactionDto(
                                                x.OrderNumber,
                                                x.OrderDate,
                                                x.Total,
                                                string.Join("\r\n", x.Items!.Select(i => $"{i.ProductName} - {i.Price} x {i.VendQty}").ToList())
                                            ))
                                        .ToListAsync();
                return new AppUserTransactionDto(true, orders, []);
            }

            if (paymentConfig.AccountConfig.Plan == StrDir.AccountPlan.PREPAID)
            {
                var response = await serverClient.GetAsync($"api/appuser/transaction?id={userId}");
                if (!response.IsSuccessStatusCode)
                {
                    string respText = await response.Content.ReadAsStringAsync();
                    logger.LogInformation(respText);
                    return new AppUserTransactionDto(false, [], []);
                }

                return await response.Content.ReadFromJsonAsync<AppUserTransactionDto>() ?? new AppUserTransactionDto(false, [], []);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
        return new AppUserTransactionDto(false, [], []);
    }

    public async Task<ResponseDto> ConfirmOrderPayment(string userId, double amount)
    {
        try
        {
            var response = await serverClient.PostAsync($"api/appuser/payment", new AppUserOrderPayment(userId, "", amount));

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                var respText = await response.Content.ReadAsStringAsync();
                logger.LogError(respText);
                return new ResponseDto(false, "Balance update failed");
            }

            return await response.Content.ReadFromJsonAsync<ResponseDto>() ?? new ResponseDto(false, "Balance update failed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            return new ResponseDto(false, "Balance update failed");
        }
    }

    public async Task<SpendDto> GetLocalSpendAsync(string userId)
    {
        try
        {
            DateTime startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            double monthSpend = await dbContext.Orders
                                            .Where(x => x.UserId == userId &&
                                                        x.OrderDate >= startDate &&
                                                        x.Status == StrDir.OrderStatus.SUCCESS)
                                            .Select(x => x.Total)
                                            .SumAsync();

            double todaySpend = await dbContext.Orders
                                            .Where(x => x.UserId == userId &&
                                                        x.OrderDate >= DateTime.Now.Date &&
                                                        x.Status == StrDir.OrderStatus.SUCCESS)
                                            .Select(x => x.Total)
                                            .SumAsync();

            return new SpendDto(todaySpend, monthSpend);
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            return new SpendDto(0, 0);
        }
    }

}
