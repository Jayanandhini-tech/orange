using CMS.API.Domains;
using CMS.API.Repository.IRepository;
using CMS.API.Services.Interfaces;
using CMS.Dto;

namespace CMS.API.Services;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork unitOfWork;
    private readonly ITenant tenant;

    public OrderService(IUnitOfWork unitOfWork, ITenant tenant)
    {
        this.unitOfWork = unitOfWork;
        this.tenant = tenant;
    }


    public async Task<Order?> GetOrderAsync(string orderId)
    {
        var order = await unitOfWork.Orders.GetOrderWithAllIncludes(orderId);
        return order;
    }

    public async Task<List<OrderItem>> GetOrderItemsAsync(string orderId)
    {
        var items = await unitOfWork.OrderItems.GetAllAsync(x => x.OrderId == orderId, includeProperties: "Product");
        return items.ToList();
    }

    public async Task<bool> UpdateOrderPaymentAsync(string orderId, string paymentId)
    {

        var order = await unitOfWork.Orders.GetAsync(x => x.Id == orderId, tracked: true);

        if (order is null)
            return false;

        int billno = await unitOfWork.Orders.GetNextBillNumber();
        string billNumber = $"{order.AppType[0]}/{billno.ToString("000000")}";

        order.OrderNumber = billNumber;
        order.PaymentId = paymentId;
        order.Status = StrDir.OrderStatus.SUCCESS;

        await unitOfWork.SaveChangesAsync();

        return true;
    }



}
