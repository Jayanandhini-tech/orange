namespace CMS.API.Dtos.VM.Response;

public record StockDisplayDto(int trayNumber, string productName, double productPrice ,int stock, int capacity, bool soldout, bool isActive);
 
