namespace CMS.Dto;

public record AppUserCreateDto(string Id, string Name, string ImgPath = "");

public record AppUserIdCardDto(string Id, string Name, string IdCardNo);

public record AppUserRegisterResponse(bool Success, string Message);

public record AppUserDto(string Id, string Name, string IdCardNo, double Balance, string ImagePath);

public record AppUserOrderPayment(string Id, string OrderNumber, double Amount);

public record AppUserBalanceDto(string Id, double Balance);

public record AppUserRefundDto(string Id, string OrderId, double RefundAmount);

public record AppUserTransactionDto(bool Success, List<AccountOrderTransactionDto> Orders, List<AccountRechargeTransactionDto> Recharges);

public record AccountOrderTransactionDto(string OrderNumber, DateTime OrderDate, double Amount, string Items);

public record AccountRechargeTransactionDto(string RechargeId, DateTime RechargeDate, double Amount, string Description);