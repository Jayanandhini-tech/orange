using CMS.API.Domains;
using CMS.API.Payments.Dtos.Gateway.HDFC;
using CMS.API.Payments.Services.Interface;
using CMS.Dto.Payments;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UPIKit;

namespace CMS.API.Payments.Controllers.Gateway;

[Route("api/payments/gateway/[controller]")]
[ApiController]
public class HdfcController : ControllerBase
{
    private readonly AppDBContext dBContext;
    private readonly IPaymentCallbackService paymentCallbackService;
    private readonly ILogger<HdfcController> logger;

    public HdfcController(AppDBContext dBContext, IPaymentCallbackService paymentCallbackService, ILogger<HdfcController> logger)
    {
        this.dBContext = dBContext;
        this.paymentCallbackService = paymentCallbackService;
        this.logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromForm] IFormCollection value)
    {
        try
        {
            foreach (var item in value)
            {
                logger.LogTrace($"Key : {item.Key}. Value : {item.Value}");
            }

            string? mRes = value["meRes"];
            string? pgMerchantId = value["pgMerchantId"];

            if (string.IsNullOrEmpty(mRes) || string.IsNullOrEmpty(pgMerchantId))
                return Ok();

            HdfcTransactionResponse? resp = await ReadCallBackStatusResponse(pgMerchantId, mRes);
            if (resp is null)
                return Ok();


            var status = resp.Status switch
            {
                "SUCCESS" => PaymentStatusCode.SUCCESS,
                "FAILURE" => PaymentStatusCode.FAILED,
                "PENDING" => PaymentStatusCode.PENDING,
                _ => PaymentStatusCode.FAILED
            };


            await paymentCallbackService.PaymentStatusUpdateAsync(resp.OrderNumber, status, resp.TxnId, $"{resp.PayerVPA}.{resp.PayerBankDetails}.{resp.Description}");
            return Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            return Ok();
        }
    }


    private async Task<HdfcTransactionResponse?> ReadCallBackStatusResponse(string merchantId, string resText)
    {
        var config = await dBContext.PgSettings.FirstOrDefaultAsync(x => x.MerchantId == merchantId);
        if (config is null)
            return null;

        var response = UPISecurity.Decrypt(resText, config.MerchantKey);

        logger.LogInformation(response);
        string[] resp = response.Trim().Split('|');
        if (resp.Length != 21)
        {
            logger.LogError($"Issue with reading response : {response}");
            return null;
        }

        return new HdfcTransactionResponse(resp[0], resp[1], Convert.ToDecimal(resp[2]), resp[3], resp[4], resp[5], resp[6], resp[7], resp[8], resp[9], resp[10], resp[16], resp[17]);
    }

}
