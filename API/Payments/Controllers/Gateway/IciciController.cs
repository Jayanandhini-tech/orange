using CMS.API.Domains;
using CMS.API.Payments.Dtos.Gateway;
using CMS.API.Payments.Dtos.Gateway.ICICI;
using CMS.API.Payments.Services;
using CMS.API.Payments.Services.Interface;
using CMS.Dto.Payments;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text;

namespace CMS.API.Payments.Controllers.Gateway;

[Route("api/payments/gateway/[controller]")]
[ApiController]
public class IciciController : ControllerBase
{
    private readonly AppDBContext dBContext;
    private readonly HttpClient httpClient;
    private readonly IServiceProvider serviceProvider;
    private readonly IPaymentCallbackService paymentCallbackService;
    private readonly ILogger<IciciController> logger;

    public IciciController(AppDBContext dBContext, HttpClient httpClient, IServiceProvider serviceProvider, IPaymentCallbackService paymentCallbackService, ILogger<IciciController> logger)
    {
        this.dBContext = dBContext;
        this.httpClient = httpClient;
        this.serviceProvider = serviceProvider;
        this.paymentCallbackService = paymentCallbackService;
        this.logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Post()
    {
        try
        {
            string body = string.Empty;
            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                body = await reader.ReadToEndAsync();
            }

            if (string.IsNullOrEmpty(body))
                return Ok();

            var config = await this.dBContext.PgSettings.FirstOrDefaultAsync(x => x.GatewayName == "ICICI");

            var iciciService = new IciciService(config.Adapt<PaymentGatewayConfigDto>(), httpClient, serviceProvider.GetRequiredService<ILogger<IciciService>>());

            var responseText = iciciService.DecryptData(body);
            logger.LogInformation(responseText);

            var callBackDto = JsonConvert.DeserializeObject<IciciPaymentCallBackDto>(responseText);

            if (callBackDto is null)
                return Ok();

            string orderNumber = callBackDto.merchantTranId;

            var status = callBackDto.TxnStatus switch
            {
                "PENDING" => PaymentStatusCode.PENDING,
                "SUCCESS" => PaymentStatusCode.SUCCESS,
                _ => PaymentStatusCode.FAILED,
            };

            await paymentCallbackService.PaymentStatusUpdateAsync(orderNumber, status, string.Empty, $"{callBackDto.PayerName} | {callBackDto.PayerMobile} | {callBackDto.PayerVA}", callBackDto.BankRRN);

        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
        return Ok();
    }
}
