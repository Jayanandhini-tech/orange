using VM.Dtos;

namespace VM.Services.Interfaces;

public interface IJuiceService
{
    Task<string> CreateOrderAsync(List<DisplayProductDto> product, OrderPaymentDto paymentDto, string deliveryType = "");
    Task<string> GetNextOrderNumberforUPI();
    Task SendOrderToServer(string orderNumber);
    Task UpdateOrderStatus(string orderNumber);
}