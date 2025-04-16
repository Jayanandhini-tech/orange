
using VM.Dtos;

namespace VM.Services.Interfaces;

public interface IOrderService
{
    Task<string> CreateOrderAsync(List<DisplayProductDto> selectedItems, OrderPaymentDto paymentDto, string deliveryType = "");
    Task<string> GetNextOrderNumberforUPI();
    Task SendOrderToServer(string orderNumber);
    Task UpdateOrderStatus(string orderNumber);
}