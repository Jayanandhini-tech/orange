namespace CMS.Dto.Payments.QR;

public record QrImageDto(string OrderNumber, bool IsSuccess = false, string QRbase64png = "", string DisplayName = "", string Message = "");