namespace CMS.Dto;


public record OrderConfirmResponseDto(bool IsSuccess, string Message, BillPrintDto? Bill);