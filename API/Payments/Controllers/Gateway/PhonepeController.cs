using CMS.API.Extensions;
using CMS.API.Payments.Dtos.Gateway.Phonepe;
using CMS.API.Payments.Services.Interface;
using CMS.Dto.Payments;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace CMS.API.Payments.Controllers.Gateway
{
    [Route("api/payments/gateway/[controller]")]
    [ApiController]
    public class PhonepeController : ControllerBase
    {
        private readonly IPaymentCallbackService paymentCallbackService;
        private readonly ILogger<PhonepeController> logger;

        public PhonepeController(IPaymentCallbackService paymentCallbackService, ILogger<PhonepeController> logger)
        {
            this.paymentCallbackService = paymentCallbackService;
            this.logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> PaymentStatus(PhonepeAPIResponse phonepeAPIResponse)
        {
            try
            {
                var responseText = phonepeAPIResponse.Response.ConvertBase64ToString();
                logger.LogInformation(responseText);

                var paymentStatus = JsonConvert.DeserializeObject<PhonePePaymentStatus>(responseText)!;


                string orderNumber = paymentStatus.Data.TransactionId;

                var status = paymentStatus.Code switch
                {
                    "PAYMENT_PENDING" => PaymentStatusCode.PENDING,
                    "PAYMENT_SUCCESS" => PaymentStatusCode.SUCCESS,
                    _ => PaymentStatusCode.FAILED,
                };


                await paymentCallbackService.PaymentStatusUpdateAsync(orderNumber, status, paymentStatus.Data.ProviderReferenceId, paymentStatus.Message);
                
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }
            return Ok();
        }

    }
}
