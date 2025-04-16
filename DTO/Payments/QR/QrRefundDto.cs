namespace CMS.Dto.Payments.QR;

public record QrRefundDto(string OrderNumber, double RefundAmount, string msg = "");