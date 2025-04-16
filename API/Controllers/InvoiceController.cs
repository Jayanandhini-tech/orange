using CMS.API.Domains;
using CMS.API.Repository.IRepository;
using CMS.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class InvoiceController : ControllerBase
{
    public IUnitOfWork unitOfWork { get; }

    public InvoiceController(IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork;
    }

    [HttpGet("id/{orderId}")]
    public async Task<IActionResult> GetInvoice(string orderId)
    {
        var order = await unitOfWork.Orders.GetOrderWithAllIncludes(orderId);
        return GetResult(order);
    }

    [HttpGet("number/{ordernumber}")]
    public async Task<IActionResult> GetInvoiceByOrderNumber(string ordernumber)
    {
        var order = await unitOfWork.Orders.GetOrderUsingOrderNumber(ordernumber);
        return GetResult(order);
    }


    private IActionResult GetResult(Order? order)
    {
        if (order is null)
            return BadRequest("Order not found");

        if (order.Payment is null)
            return BadRequest("Payment not done");

        if (!order.Payment.IsPaid)
            return BadRequest("Payment not done");

        return Ok(GetBillingDto(order));
    }

    private BillPrintDto GetBillingDto(Order order)
    {
        double subTotal = Math.Round(order.Items!.Sum(x => x.Rate * x.VendQty), 2);
        double gst = Math.Round(order.Items!.Select(x => ((x.Rate * x.VendQty) * x.Gst) / 100).Sum(), 2);
        double cgst = Math.Round(gst / 2, 2);
        double sgst = cgst;
        double beforeRound = Math.Round((subTotal + cgst + sgst), 2);
        double roundoff = Math.Round(order.Total - beforeRound, 2);


        BillPrintDto billPrintDto = new BillPrintDto(
            new Dto.Company(StrDir.Company.Name, StrDir.Company.Address, StrDir.Company.Phone, StrDir.Company.GstIn),
            new BillHeader(order.OrderNumber, order.OrderDate, order.MachineId, order.DeliveryType, order.Id),
             order.Items!.Select(x => new Items(x.ProductName, x.Rate, x.Gst, x.Price,x.Qty)).ToList(),
            new Calculation(subTotal, cgst, sgst, roundoff, order.Total),
            new PaymentDto(order.Payment!.PaymentType, order.Payment.Reference, order.Payment.Amount, order.Payment.RefundAmount)
            );

        return billPrintDto;
    }
}
