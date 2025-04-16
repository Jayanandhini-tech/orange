using CMS.API.Domains;
using CMS.API.Extensions;
using CMS.API.Repository.IRepository;
using CMS.API.Services.Interfaces;
using CMS.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;


namespace CMS.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class OrderController : ControllerBase
{

    private readonly ITenant tenant;
    private readonly IUnitOfWork unitOfWork;
    private readonly ILogger<OrderController> logger;

    public OrderController(ITenant tenant, IUnitOfWork unitOfWork, ILogger<OrderController> logger)
    {
        this.tenant = tenant;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }


    [HttpGet("sales")]
    public async Task<IActionResult> GetSalesRecords(string machineId, DateTime fromDate, DateTime toDate)
    {
        if (machineId.Trim().ToLower() == "all")
            machineId = string.Empty;

        var result = await unitOfWork.Orders.GetSalesReportAsync(machineId, fromDate, toDate);
        return Ok(result);
    }

    [HttpGet("sales/download")]
    public async Task<IActionResult> GetSalesReport(string machineId, DateTime fromDate, DateTime toDate)
    {
        if (machineId.Trim().ToLower() == "all")
            machineId = string.Empty;

        var sales = await unitOfWork.Orders.GetSalesReportAsync(machineId, fromDate, toDate);

        var result = sales.Select(x => new
        {
            Order = x.OrderNumber,
            Date = x.OrderDate,
            Type = x.PaymentType,
            x.Amount,
            x.Paid,
            x.Refund,
            Items = string.Join("\n", x.OrderItems!.Select(x => $"{x.ProductName} - {x.Price} x {x.Quantity} = {x.Amount}").ToList()),
            x.UserId
        }).ToList();

        var file = ExcelHelper.CreateFile(result, $"Sales Report Between {fromDate.ToString("MMM dd yyyy hh:mm tt")} and {toDate.ToString("MMM dd yyyy hh:mm tt")}");
        return File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "StockReport.xlsx");
    }


    [HttpGet("sales/product")]
    public async Task<IActionResult> GetSalesBasedOnProductRecords(string machineId, DateTime fromDate, DateTime toDate)
    {
        if (machineId.Trim().ToLower() == "all")
            machineId = string.Empty;

        var result = await unitOfWork.Orders.GetProductBasedSales(machineId, fromDate, toDate);
        return Ok(result);
    }

    [HttpGet("sales/product/download")]
    public async Task<IActionResult> GetSalesBasedOnProductReport(string machineId, DateTime fromDate, DateTime toDate)
    {
        if (machineId.Trim().ToLower() == "all")
            machineId = string.Empty;

        var result = await unitOfWork.Orders.GetProductBasedSales(machineId, fromDate, toDate);

        var file = ExcelHelper.CreateFile(result, $"Sales Product Based Report Between {fromDate.ToString("MMM dd yyyy hh:mm tt")} and {toDate.ToString("MMM dd yyyy hh:mm tt")}");
        return File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "SalesProductsReport.xlsx");
    }


    [HttpGet("sales/user")]
    public async Task<IActionResult> GetSalesBasedOnUserRecords(string machineId, DateTime fromDate, DateTime toDate)
    {
        if (machineId.Trim().ToLower() == "all")
            machineId = string.Empty;

        var result = await unitOfWork.Orders.GetUserBasedSales(machineId, fromDate, toDate);
        return Ok(result);
    }


    [HttpGet("sales/user/download")]
    public async Task<IActionResult> GetSalesBasedOnUserReport(string machineId, DateTime fromDate, DateTime toDate)
    {
        if (machineId.Trim().ToLower() == "all")
            machineId = string.Empty;

        var records = await unitOfWork.Orders.GetUserBasedSales(machineId, fromDate, toDate);
        var result = records.Select(x => new { x.UserId, x.Name, x.Amount, Items = string.Join("\n", x.Items) }).ToList();
        var file = ExcelHelper.CreateFile(result, $"Sales User Based Report Between {fromDate.ToString("MMM dd yyyy hh:mm tt")} and {toDate.ToString("MMM dd yyyy hh:mm tt")}");
        return File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "SalesUserReport.xlsx");
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrUpdateOrder(OrderDto orderDto)
    {
        try
        {
            var order = await unitOfWork.Orders.GetOrderUsingOrderNumber(orderDto.OrderNumber, true);

            if (order is null)
                order = await CreateOrder(orderDto);

            order.Total = orderDto.Total;
            order.Status = orderDto.Status;
            order.IsPaid = orderDto.IsPaid;
            order.PaidAmount = orderDto.PaidAmount;
            order.IsRefunded = orderDto.IsRefunded;
            order.RefundedAmount = orderDto.RefundedAmount;
            order.AppUserId = orderDto.UserId;
            order.Reference = orderDto.Reference;

            if (!string.IsNullOrEmpty(orderDto.PaymentId.Trim()))
            {
                if (string.IsNullOrEmpty(order.PaymentId))
                    order.PaymentId = orderDto.PaymentId;
            }

            foreach (var item in orderDto.Items)
            {
                var oItem = order.Items!.FirstOrDefault(x => x.Id == item.Id);
                if (oItem is not null)
                {
                    oItem.VendQty = item.VendQty;
                    oItem.UpdatedOn = item.UpdatedOn;
                }
            }

            await unitOfWork.SaveChangesAsync();

            return Ok(new OrderNumberDto(order.OrderNumber));
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }


    [HttpPost("sync")]
    public async Task<IActionResult> OrdersAsync(List<OrderDto> ordersDto)
    {
        List<string> successOrders = new List<string>();

        var limitedOrders = ordersDto.OrderBy(x => x.OrderDate).Take(10).ToList();
        try
        {
            foreach (OrderDto orderDto in limitedOrders)
            {
                try
                {
                    Order? order = await unitOfWork.Orders.GetOrderUsingOrderNumber(orderDto.OrderNumber, true);

                    if (order is null)
                        order = await CreateOrder(orderDto);


                    order.Total = orderDto.Total;
                    order.Status = orderDto.Status;
                    order.IsPaid = orderDto.IsPaid;
                    order.PaidAmount = orderDto.PaidAmount;
                    order.IsRefunded = orderDto.IsRefunded;
                    order.RefundedAmount = orderDto.RefundedAmount;
                    order.AppUserId = orderDto.UserId;
                    order.Reference = orderDto.Reference;

                    if (!string.IsNullOrEmpty(orderDto.PaymentId.Trim()))
                    {
                        if (string.IsNullOrEmpty(order.PaymentId))
                            order.PaymentId = orderDto.PaymentId;
                    }

                    foreach (var item in orderDto.Items)
                    {
                        var oItem = order.Items!.FirstOrDefault(x => x.Id == item.Id);
                        if (oItem is not null)
                        {
                            oItem.VendQty = item.VendQty;
                            oItem.UpdatedOn = item.UpdatedOn;
                        }
                    }

                    await unitOfWork.SaveChangesAsync();
                    successOrders.Add(orderDto.OrderNumber);

                }
                catch (DbUpdateException updateex)
                {
                    logger.LogError($"Error Record : {JsonConvert.SerializeObject(orderDto)}");
                    logger.LogError(updateex.Message);
                    if (updateex.InnerException is not null)
                        logger.LogError(updateex.InnerException, updateex.InnerException.Message);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }

        return Ok(new OrderSyncResultDto(successOrders));
    }

    [HttpPost("kiosk/stockupdate")]
    public async Task<IActionResult> KisokStockUpdateAsync(OrderDespatchDto orderDespatch)
    {
        try
        {
            foreach (var item in orderDespatch.DespatchItems)
            {
                var product = await unitOfWork.Products.GetAsync(x => x.Id == item.ProductId, tracked: true);
                if (product is not null)
                {
                    product.Stock = product.Stock - item.VendQty;
                }
            }
            await unitOfWork.SaveChangesAsync();

            return Ok(new OrderNumberDto(orderDespatch.OrderNumber));
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }



    private async Task<Order> CreateOrder(OrderDto orderDto)
    {
        Order order = new Order()
        {
            Id = orderDto.Id,
            OrderNumber = orderDto.OrderNumber,
            OrderDate = orderDto.OrderDate,
            DeliveryType = orderDto.DeliveryType,
            PaymentType = orderDto.PaymentType,
            Status = StrDir.OrderStatus.INITIATED,
            Total = orderDto.Total,
            IsPaid = false,
            PaidAmount = 0,
            IsRefunded = false,
            RefundedAmount = 0,
            MachineId = tenant.MachineId,
            Items = orderDto.Items.Select(i =>
                        new OrderItem()
                        {
                            Id = i.Id,
                            OrderId = i.OrderId,
                            ProductId = i.ProductId,
                            ProductName = i.ProductName,
                            Rate = i.Rate,
                            Gst = i.Gst,
                            Price = i.Price,
                            Qty = i.Qty,
                            VendQty = i.VendQty,
                            AppType = tenant.AppType,
                            VendorId = tenant.VendorId,
                            UpdatedOn = DateTime.Now,
                        }).ToList()
        };

        await unitOfWork.Orders.AddAsync(order);
        await unitOfWork.SaveChangesAsync();
        return order;
    }

}
