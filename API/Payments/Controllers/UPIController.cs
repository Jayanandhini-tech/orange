using CMS.API.Domains;
using CMS.API.Payments.Dtos;
using CMS.API.Payments.Services;
using CMS.API.Payments.Services.Interface;
using CMS.API.Repository.IRepository;
using CMS.API.Services.Interfaces;
using CMS.Dto;
using CMS.Dto.Payments;
using CMS.Dto.Payments.QR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CMS.API.Payments.Controllers
{
    [Route("api/payments/[controller]")]
    [ApiController]
    [Authorize]
    public class UPIController : ControllerBase
    {
        private readonly IPaymentService paymentService;
        private readonly ITenant tenant;
        private readonly IDynamicQRCodeGenerator qrGenerator;
        private readonly IUnitOfWork db;
        private readonly ILogger<UPIController> logger;

        public UPIController(IPaymentService paymentService, ITenant tenant, IDynamicQRCodeGenerator qrGenerator, IUnitOfWork db, ILogger<UPIController> logger)
        {
            this.paymentService = paymentService;
            this.tenant = tenant;
            this.qrGenerator = qrGenerator;
            this.db = db;
            this.logger = logger;
        }


        [HttpPost("create")]
        public async Task<IActionResult> CreateQR(QrCreateDto qrDto)
        {
            var exist = await db.Payments.AnyAsync(x => x.OrderNumber == qrDto.OrderNumber);

            if (exist)
                return Ok(new QrImageDto(qrDto.OrderNumber, Message: "Order already exist. Try again"));

            UPICreateResponse response = await paymentService.CreateQRAsync(qrDto.OrderNumber, qrDto.Amount, tenant.MachineName);

            if (!response.Success)
                return Ok(new QrImageDto(qrDto.OrderNumber, Message: response.Message));

            Payment payment = new Payment()
            {
                Id = $"py_{Guid.NewGuid().ToString("N").ToUpper()}",
                OrderNumber = qrDto.OrderNumber,
                Amount = qrDto.Amount,
                PaymentType = StrDir.PaymentType.UPI,
                PaymentProvider = paymentService.Gateway,
                Status = PaymentStatusCode.PENDING.ToString(),
                TransactionId = response.TransactionId == qrDto.OrderNumber ? "" : response.TransactionId,
                PaymentOn = DateTime.Now,
                MachineId = tenant.MachineId
            };

            await db.Payments.AddAsync(payment);
            await db.SaveChangesAsync();

            string qr = qrGenerator.CreateQRfromUPIstring(response.UpiString);

            return Ok(new QrImageDto(qrDto.OrderNumber, true, qr, response.DisplayName));
        }

        [HttpGet("status")]
        public async Task<IActionResult> Status(string orderNumber)
        {

            var payment = await db.Payments.GetAsync(x => x.OrderNumber == orderNumber);

            if (payment is null)
                return Ok(new QrStatusDto(orderNumber, PaymentStatusCode.FAILED, "OrderId not found", ""));

            return Ok(new QrStatusDto(orderNumber, Enum.Parse<PaymentStatusCode>(payment.Status), "", payment.Id));

        }

        [HttpGet("status/gateway")]
        public async Task<IActionResult> StatusonGateway(string orderNumber)
        {

            UPIStatusResponse response = await paymentService.StatusAsync(orderNumber);

            if (!response.Success)
                return Ok(new QrStatusDto(orderNumber, PaymentStatusCode.FAILED, response.Message, ""));


            var requester = await db.Payments.UpdatePaymentStatus(orderNumber, response.Status.ToString(), response.TransactionId, response.Message, response.BankRRR);

            return Ok(new QrStatusDto(orderNumber, response.Status, response.Message, requester?.PaymentId ?? ""));
        }


        [HttpPost("refund")]
        public async Task<IActionResult> Refund(QrRefundDto refundDto)
        {
            logger.LogInformation(JsonSerializer.Serialize(refundDto));

            var payment = await db.Payments.GetAsync(x => x.OrderNumber == refundDto.OrderNumber);

            if (payment is null)
                return Ok(new SuccessDto("Payment not found"));

            if (payment.IsPaid == false)
                return Ok(new SuccessDto("Payment not done"));

            if (payment.IsRefunded)
                return Ok(new SuccessDto("Refund already done"));

            var response = await paymentService.RefundAsync(refundDto.OrderNumber, payment.TransactionId, refundDto.RefundAmount, refundDto.msg, payment.BankRRN);

            if (response.Success)
                await db.Payments.UpdateRefundStatus(refundDto.OrderNumber, response.RefundId, refundDto.RefundAmount, response.RefundRefId);

            return Ok(new SuccessDto("Successfully Refunded"));
        }

    }
}