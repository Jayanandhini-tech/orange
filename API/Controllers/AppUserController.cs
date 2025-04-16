using CMS.API.Domains;
using CMS.API.Repository.IRepository;
using CMS.Dto;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AppUserController : ControllerBase
{
    private readonly IUnitOfWork unitOfWork;
    private readonly ILogger<AppUserController> logger;

    public AppUserController(IUnitOfWork unitOfWork, ILogger<AppUserController> logger)
    {
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }



    [HttpGet("idcard/{idCardNo}")]
    public async Task<IActionResult> GetDetails(string idCardNo)
    {
        var user = await unitOfWork.AppUsers.GetAsync(x => x.IdCardNo == idCardNo);
        if (user == null)
            return BadRequest("Idcard not found");

        return Ok(user.Adapt<AppUserDto>());
    }

    [HttpGet("balance")]
    public async Task<IActionResult> GetUserBalance(string id, string plan)
    {
        var user = await unitOfWork.AppUsers.GetAsync(x => x.Id == id);
        if (user == null)
            return Ok(new AppUserBalanceDto(id, 0));

        if (plan == StrDir.AccountPlan.PREPAID)
            return Ok(new AppUserBalanceDto(id, user.Balance));

        DateTime startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var orders = await unitOfWork.Orders.GetAllAsync(x => x.AppUserId == user.Id && x.Status == StrDir.OrderStatus.SUCCESS && x.OrderDate >= startDate);
        double spend = orders.Sum(x => x.Total);

        return Ok(new AppUserBalanceDto(id, user.CreditLimit - spend));
    }

    [HttpGet("transaction")]
    public async Task<IActionResult> GetUserTransaction(string id)
    {
        var appUser = await unitOfWork.AppUsers.GetAsync(x => x.Id == id);
        if (appUser is null)
            return Ok(new AppUserTransactionDto(false, [], []));

        var userOrders = await unitOfWork.Orders.GetUserRecentOrders(id);
        var orderTrans = userOrders.Select(x =>
                                new AccountOrderTransactionDto(
                                    x.OrderNumber,
                                    x.OrderDate,
                                    x.Total,
                                    string.Join("\r\n", x.Items?.Select(i => $"{i.ProductName} - {i.Price} x {i.VendQty}").ToList() ?? []))).ToList();

        var recharges = await unitOfWork.Recharges.GetUserRecentRechargeAsync(id);
        var rechargeTrans = recharges.Select(x => new AccountRechargeTransactionDto(x.Id, x.RechargedOn, x.Amount, $"{x.PaymentType} - {x.TransactionId}")).ToList();

        return Ok(new AppUserTransactionDto(true, orderTrans, rechargeTrans));
    }


    [HttpPost]
    public async Task<IActionResult> CreateNewUser(AppUserCreateDto appUserDto)
    {
        bool idExist = await unitOfWork.AppUsers.AnyAsync(x => x.Id == appUserDto.Id);
        if (idExist)
            return Ok(new AppUserRegisterResponse(true, "UserId already exist"));

        AppUser appUser = new AppUser()
        {
            Id = appUserDto.Id,
            Name = appUserDto.Name,
            ImagePath = appUserDto.ImgPath,
            IdCardNo = "",
            Balance = 0,
            CreditLimit = 2500,
            CreatedOn = DateTime.Now,
            IsActive = true
        };

        await unitOfWork.AppUsers.AddAsync(appUser);
        await unitOfWork.SaveChangesAsync();
        return Ok(new AppUserRegisterResponse(true, "User created successfully"));
    }

    [HttpPost("sync")]
    public async Task<IActionResult> AppUserUpdateSync(List<AppUserCreateDto> appUsersDto)
    {
        List<string> successIds = [];
        try
        {
            foreach (var appUserDto in appUsersDto)
            {

                bool isExist = await unitOfWork.AppUsers.AnyAsync(x => x.Id == appUserDto.Id);
                if (isExist)
                {
                    successIds.Add(appUserDto.Id);
                }
                else
                {
                    AppUser appUser = new AppUser()
                    {
                        Id = appUserDto.Id,
                        Name = appUserDto.Name,
                        ImagePath = appUserDto.ImgPath,
                        IdCardNo = "",
                        Balance = 0,
                        CreditLimit = 2500,
                        CreatedOn = DateTime.Now,
                        IsActive = true
                    };

                    await unitOfWork.AppUsers.AddAsync(appUser);
                    await unitOfWork.SaveChangesAsync();
                    successIds.Add(appUserDto.Id);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
        return Ok(successIds);
    }


    [HttpPost("idcard")]
    public async Task<IActionResult> RegisterAsync(AppUserIdCardDto appUserDto)
    {
        bool idExist = await unitOfWork.AppUsers.AnyAsync(x => x.Id == appUserDto.Id);
        if (idExist)
            return Ok(new AppUserRegisterResponse(false, "Roll number already exist"));

        bool idCardExist = await unitOfWork.AppUsers.AnyAsync(x => x.IdCardNo == appUserDto.IdCardNo);

        if (idCardExist)
            return Ok(new AppUserRegisterResponse(false, "This Id card was already registered"));


        AppUser appUser = new AppUser()
        {
            Id = appUserDto.Id,
            Name = appUserDto.Name,
            IdCardNo = appUserDto.IdCardNo,
            Balance = 0,
            CreditLimit = 1000,
            CreatedOn = DateTime.Now,
            ImagePath = string.Empty,
            IsActive = true
        };

        await unitOfWork.AppUsers.AddAsync(appUser);
        await unitOfWork.SaveChangesAsync();

        return Ok(new AppUserRegisterResponse(true, "This Id card is registered successfully"));
    }

    [HttpPost("payment")]
    public async Task<IActionResult> OrderPayment(AppUserOrderPayment paymentDto)
    {
        var user = await unitOfWork.AppUsers.GetAsync(x => x.Id == paymentDto.Id, tracked: true);
        if (user is null)
            return Ok(new ResponseDto(false, "User not found"));

        if (user.Balance < paymentDto.Amount)
            return Ok(new ResponseDto(false, "Sorry, you do not have enough balance to process this order."));

        //var order = await unitOfWork.Orders.GetAsync(x => x.OrderNumber == paymentDto.OrderNumber, tracked: true);
        //if (order is null)
        //    return Ok(new ResponseDto(false, "Order not found"));

        //if (order.IsPaid)
        //    return Ok(new ResponseDto(false, "The payment was already done"));

        user.Balance = user.Balance - paymentDto.Amount;

        //order.AppUserId = user.Id;
        //order.IsPaid = true;
        //order.PaidAmount = paymentDto.Amount;
        //order.Status = StrDir.OrderStatus.PAID;

        await unitOfWork.SaveChangesAsync();

        return Ok(new ResponseDto(true, "Payment Success"));
    }

    [HttpPost("refund")]
    public async Task<IActionResult> OrderRefund(AppUserRefundDto refundDto)
    {
        var user = await unitOfWork.AppUsers.GetAsync(x => x.Id == refundDto.Id, tracked: true);
        if (user is null)
            return Ok(new ResponseDto(false, "User not found"));

        var order = await unitOfWork.Orders.GetAsync(x => x.Id == refundDto.OrderId, tracked: true);
        if (order is null)
            return Ok(new ResponseDto(false, "Order not found"));

        if (order.IsRefunded)
            return Ok(new ResponseDto(false, "The refund was already done"));

        user.Balance = user.Balance + refundDto.RefundAmount;

        order.AppUserId = user.Id;
        order.IsRefunded = true;
        order.RefundedAmount = refundDto.RefundAmount;

        await unitOfWork.SaveChangesAsync();

        return Ok(new ResponseDto(true, "Refund Success"));
    }


}
