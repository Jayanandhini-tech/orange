namespace CMS.Dto.Payments.QR;

// 0 - Pending, 1 - Success, 2 - Failed
public record QrStatusDto(string OrderNumber, PaymentStatusCode Status, string Message = "", string PaymentId = "");