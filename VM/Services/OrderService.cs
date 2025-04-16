using CMS.Dto;
using log4net.Repository.Hierarchy;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VM.Domains;
using VM.Dtos;
using VM.Services.Interfaces;

namespace VM.Services;

public class OrderService : IOrderService
{
    private readonly AppDbContext dbContext;
    private readonly IServerClient httpClient;
    private readonly ILogger<OrderService> logger;


    public OrderService(AppDbContext dbContext, IServerClient httpClient, ILogger<OrderService> logger)
    {
        this.dbContext = dbContext;
        this.httpClient = httpClient;
        this.logger = logger;

    }

    public async Task<string> GetNextOrderNumberforUPI()
    {
        var lastOrder = await dbContext.Orders.OrderByDescending(x => x.OrderDate).FirstOrDefaultAsync();
        int.TryParse(lastOrder?.OrderNumber[^6..], out int orderCount);
        orderCount++;

        Random random = new Random();
        int tmp = random.Next(101, 999);

        string orderNumber = $"{DataStore.MachineInfo.VendorShortName}{DataStore.MachineInfo.MachineNumber.ToString("00")}-{tmp}-{DataStore.AppType[0]}{orderCount.ToString("000000")}";
        return orderNumber;
    }

    //public async Task<string> CreateOrderAsync(List<DisplayProductDto> products, OrderPaymentDto paymentDto, string deliveryType = "")
    //{
    //    try
    //    {
    //        string orderId = $"or_{Guid.NewGuid().ToString("N").ToUpper()}";


    //        var lastOrder = await dbContext.Orders.OrderByDescending(x => x.OrderDate).FirstOrDefaultAsync();
    //        int.TryParse(lastOrder?.OrderNumber[^6..], out int orderCount);
    //        orderCount++;

    //        string orderNumber = $"{DataStore.MachineInfo.VendorShortName}{DataStore.MachineInfo.MachineNumber.ToString("00")}{DataStore.AppType[0]}{orderCount.ToString("000000")}";

    //        var product = await dbContext.Products
    //                                              .Where(x => EF.Functions.Like(x.Name, "%juice%"))
    //                                              .OrderByDescending(x => x.UpdatedOn)
    //                                              .FirstOrDefaultAsync();

    //        logger.LogError("DDDD selected product");

    //        logger.LogError($"{product.Price}");



    //        Order order = new Order()
    //        {
    //            Id = orderId,
    //            OrderNumber = orderNumber,
    //            OrderDate = DateTime.Now,
    //            PaymentType = paymentDto.PaymentType,
    //            DeliveryType = string.IsNullOrEmpty(deliveryType) ? "VEND" : deliveryType,  // Default to "VEND" if deliveryType is not provided
    //            IsPaid = paymentDto.IsPaid,
    //            PaidAmount = paymentDto.Amount,
    //            IsRefunded = false,
    //            Total = product.Price,
    //            RefundedAmount = 0,
    //            UserId = paymentDto.UserId,
    //            Reference = paymentDto.Reference.Length > 49 ? paymentDto.Reference.Substring(0, 49) : paymentDto.Reference,
    //            PaymentId = paymentDto.PaymentId,
    //            Status = paymentDto.IsPaid ? StrDir.OrderStatus.PAID : StrDir.OrderStatus.INITIATED,
    //            IsViewed = false,

    //            Items = product.Select(p =>
    //                  [new OrderItem()
    //                  {
    //                      OrderId = orderId,
    //                      ProductId = product.Id,
    //                      ProductName = product.Name,
    //                      Rate = product.BaseRate,
    //                      Gst = product.Gst,
    //                      Price = product.Price,
    //                      Qty = 1,
    //                      VendQty = 0,
    //                      IsViewed = false,
    //                      UpdatedOn = DateTime.Now
    //                  }])
    //        };

    //        // Add order to DB and save changes
    //        await dbContext.Orders.AddAsync(order);
    //        await dbContext.SaveChangesAsync();


    //        logger.LogInformation("\n\n");
    //        logger.LogInformation("----------------------------------------------------------------------------");
    //        logger.LogInformation($"Order Number : {order.OrderNumber}, Amount : {order.Total}, Type : {order.PaymentType}");

    //        return orderNumber;
    //    }
    //    catch (Exception ex)
    //    {
    //        // Log the exception and return a fallback order number
    //        logger.LogError(ex.Message, ex);
    //        return $"{DataStore.MachineInfo.VendorShortName}{DataStore.MachineInfo.MachineNumber.ToString("00")}{DataStore.AppType[0]}{Guid.NewGuid().ToString("N").ToUpper().Substring(0, 6)}";
    //    }
    //}

    public async Task<string> CreateOrderAsync(List<DisplayProductDto> selectedItems, OrderPaymentDto paymentDto, string deliveryType = "")
    {
        try
        {
            string orderId = $"or_{Guid.NewGuid().ToString("N").ToUpper()}";

            var lastOrder = await dbContext.Orders.OrderByDescending(x => x.OrderDate).FirstOrDefaultAsync();
            int.TryParse(lastOrder?.OrderNumber[^6..], out int orderCount);
            orderCount++;

            string orderNumber = $"{DataStore.MachineInfo.VendorShortName}{DataStore.MachineInfo.MachineNumber.ToString("00")}{DataStore.AppType[0]}{orderCount.ToString("000000")}";

            //var product = await dbContext.Products
            //                              .Where(x => EF.Functions.Like(x.Name, "%juice%"))
            //                              .OrderByDescending(x => x.UpdatedOn)
            //                              .FirstOrDefaultAsync();

            var product = dbContext.Products.OrderByDescending(x => x.UpdatedOn).First();

            logger.LogError($"{product.Price}");

            Order order = new Order()
            {
                Id = orderId,
                OrderNumber = orderNumber,
                OrderDate = DateTime.Now,
                PaymentType = paymentDto.PaymentType,
                DeliveryType = string.IsNullOrEmpty(deliveryType) ? "VEND" : deliveryType,
                IsPaid = paymentDto.IsPaid,
                PaidAmount = paymentDto.Amount,
                IsRefunded = false,
                Total = product.Price,
                RefundedAmount = 0,
                UserId = paymentDto.UserId,
                Reference = paymentDto.Reference.Length > 49 ? paymentDto.Reference.Substring(0, 49) : paymentDto.Reference,
                PaymentId = paymentDto.PaymentId,
                Status = paymentDto.IsPaid ? StrDir.OrderStatus.PAID : StrDir.OrderStatus.INITIATED,
                IsViewed = false,
                Items = new List<OrderItem>
            {
                new OrderItem()
                {
                    Id = $"oi_{Guid.NewGuid().ToString("N").ToUpper()}",
                    OrderId = orderId,
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Rate = product.BaseRate,
                    Gst = product.Gst,
                    Price = product.Price,
                    Qty = 1,
                    VendQty = 0,
                    IsViewed = false,
                    UpdatedOn = DateTime.Now
                }
            }
            };

            // Add order to DB and save changes
            await dbContext.Orders.AddAsync(order);
            await dbContext.SaveChangesAsync();

            logger.LogInformation("\n\n");
            logger.LogInformation("----------------------------------------------------------------------------");
            logger.LogInformation($"Order Number : {order.OrderNumber}, Amount : {order.Total}, Type : {order.PaymentType}");

            return orderNumber;
        }
        catch (Exception ex)
        {
            // Log the exception and return a fallback order number
            logger.LogError(ex.Message, ex);
            return $"{DataStore.MachineInfo.VendorShortName}{DataStore.MachineInfo.MachineNumber.ToString("00")}{DataStore.AppType[0]}{Guid.NewGuid().ToString("N").ToUpper().Substring(0, 6)}";
        }
    }


    public async Task UpdateOrderStatus(string orderNumber)
    {
        try
        {
            var order = await dbContext.Orders.Include(x => x.Items).FirstOrDefaultAsync(x => x.OrderNumber == orderNumber);
            if (order is not null)
            {
                if (!(order.Status == StrDir.OrderStatus.SUCCESS || order.Status == StrDir.OrderStatus.FAILED))
                {
                    var totalVend = order.Items?.Sum(x => x.VendQty * x.Price);
                    order.Total = totalVend ?? 0;

                    if (totalVend > 0)
                        order.Status = StrDir.OrderStatus.SUCCESS;
                    else
                        order.Status = StrDir.OrderStatus.FAILED;

                    await dbContext.SaveChangesAsync();
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    public async Task SendOrderToServer(string orderNumber)
    {
        try
        {
            var order = await dbContext.Orders.Include(x => x.Items).FirstOrDefaultAsync(x => x.OrderNumber == orderNumber);

            if (order is null)
                return;

            var dto = order.Adapt<OrderDto>();

            var response = await httpClient.PostAsync("/api/order", order);

            if (response.IsSuccessStatusCode)
            {
                order.IsViewed = true;

                foreach (var item in order.Items!)
                {
                    item.IsViewed = true;
                }
                await dbContext.SaveChangesAsync();
            }
            else
            {
                var respText = await response.Content.ReadAsStringAsync();
                logger.LogInformation(respText);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

}








//using CMS.Dto;
//using Mapster;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Logging;
//using VM.Domains;
//using VM.Dtos;
//using VM.Services.Interfaces;

//namespace VM.Services;

//public class OrderService : IOrderService
//{
//    private readonly AppDbContext dbContext;
//    private readonly IServerClient httpClient;
//    private readonly ILogger<OrderService> logger;

//    public OrderService(AppDbContext dbContext, IServerClient httpClient, ILogger<OrderService> logger)
//    {
//        this.dbContext = dbContext;
//        this.httpClient = httpClient;
//        this.logger = logger;
//    }

//    public async Task<string> GetNextOrderNumberforUPI()
//    {
//        var lastOrder = await dbContext.Orders.OrderByDescending(x => x.OrderDate).FirstOrDefaultAsync();
//        int.TryParse(lastOrder?.OrderNumber[^6..], out int orderCount);
//        orderCount++;

//        Random random = new Random();
//        int tmp = random.Next(101, 999);

//        string orderNumber = $"{DataStore.MachineInfo.VendorShortName}{DataStore.MachineInfo.MachineNumber.ToString("00")}-{tmp}-{DataStore.AppType[0]}{orderCount.ToString("000000")}";
//        return orderNumber;
//    }

//    public async Task<string> CreateOrderAsync(List<DisplayProductDto> selectedItems, OrderPaymentDto paymentDto, string deliveryType = "")
//    {
//        string orderId = $"or_{Guid.NewGuid().ToString("N").ToUpper()}";
//        var lastOrder = await dbContext.Orders.OrderByDescending(x => x.OrderDate).FirstOrDefaultAsync();
//        int.TryParse(lastOrder?.OrderNumber[^6..], out int orderCount);
//        orderCount++;
//        string orderNumber = $"{DataStore.MachineInfo.VendorShortName}{DataStore.MachineInfo.MachineNumber.ToString("00")}{DataStore.AppType[0]}{orderCount.ToString("000000")}";
//        Order order = new Order()
//        {
//            Id = orderId,
//            OrderNumber = orderNumber,
//            OrderDate = DateTime.Now,
//            PaymentType = paymentDto.PaymentType,
//            DeliveryType = deliveryType,
//            Total = selectedItems.Sum(x => x.amount),
//            IsPaid = paymentDto.IsPaid,
//            PaidAmount = paymentDto.Amount,
//            IsRefunded = false,
//            RefundedAmount = 0,
//            UserId = paymentDto.UserId,
//            Reference = paymentDto.Reference.Length > 49 ? paymentDto.Reference.Substring(0, 49) : paymentDto.Reference,
//            PaymentId = paymentDto.PaymentId,
//            Status = paymentDto.IsPaid ? StrDir.OrderStatus.PAID : StrDir.OrderStatus.INITIATED,
//            IsViewed = false,
//            Items = selectedItems.Where(x => x.qty > 0).Select(p =>
//                        new OrderItem()
//                        {
//                            Id = $"oi_{Guid.NewGuid().ToString("N").ToUpper()}",
//                            OrderId = orderId,
//                            ProductId = p.Id,
//                            ProductName = p.Name,
//                            Rate = p.Rate,
//                            Gst = p.Gst,
//                            Price = p.Price,
//                            Qty = p.qty,
//                            VendQty = 0,
//                            IsViewed = false,
//                            UpdatedOn = DateTime.Now
//                        }).ToList(),

//        };

//        await dbContext.Orders.AddAsync(order);
//        await dbContext.SaveChangesAsync();

//        return orderNumber;
//    }

//    public async Task UpdateOrderStatus(string orderNumber)
//    {
//        try
//        {
//            var order = await dbContext.Orders.Include(x => x.Items).FirstOrDefaultAsync(x => x.OrderNumber == orderNumber);
//            if (order is not null)
//            {
//                if (!(order.Status == StrDir.OrderStatus.SUCCESS || order.Status == StrDir.OrderStatus.FAILED))
//                {
//                    var totalVend = order.Items?.Sum(x => x.VendQty * x.Price);
//                    order.Total = totalVend ?? 0;

//                    if (totalVend > 0)
//                        order.Status = StrDir.OrderStatus.SUCCESS;
//                    else
//                        order.Status = StrDir.OrderStatus.FAILED;

//                    await dbContext.SaveChangesAsync();
//                }
//            }
//        }
//        catch (Exception ex)
//        {
//            logger.LogError(ex.Message, ex);
//        }
//    }

//    public async Task SendOrderToServer(string orderNumber)
//    {
//        try
//        {
//            var order = await dbContext.Orders.Include(x => x.Items).FirstOrDefaultAsync(x => x.OrderNumber == orderNumber);

//            if (order is null)
//                return;

//            var dto = order.Adapt<OrderDto>();

//            var response = await httpClient.PostAsync("/api/order", order);

//            if (response.IsSuccessStatusCode)
//            {
//                order.IsViewed = true;

//                foreach (var item in order.Items!)
//                {
//                    item.IsViewed = true;
//                }
//                await dbContext.SaveChangesAsync();
//            }
//            else
//            {
//                var respText = await response.Content.ReadAsStringAsync();
//                logger.LogInformation(respText);
//            }
//        }
//        catch (Exception ex)
//        {
//            logger.LogError(ex.Message, ex);
//        }
//    }

//}
