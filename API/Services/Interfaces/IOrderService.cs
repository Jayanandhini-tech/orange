using CMS.API.Domains;

namespace CMS.API.Services.Interfaces;

public interface IOrderService
{
    Task<Order?> GetOrderAsync(string orderId);
    Task<List<OrderItem>> GetOrderItemsAsync(string orderId);
   
    Task<bool> UpdateOrderPaymentAsync(string orderId, string paymentId);
}
