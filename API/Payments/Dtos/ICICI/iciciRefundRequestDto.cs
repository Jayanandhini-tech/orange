namespace CMS.API.Payments.Dtos.ICICI;

public record iciciRefundRequestDto(string orderNumber, string bankRRN, decimal refundAmount);

