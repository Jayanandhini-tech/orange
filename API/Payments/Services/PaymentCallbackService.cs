using CMS.API.Payments.Services.Interface;
using CMS.API.Repository.IRepository;
using CMS.API.Services.Interfaces;
using CMS.Dto.Payments;
using CMS.Dto.Payments.QR;

namespace CMS.API.Payments.Services;

public class PaymentCallbackService : IPaymentCallbackService
{
    private readonly IUnitOfWork unitOfWork;
    private readonly INotificationService notificationService;
    private readonly ILogger<PaymentCallbackService> logger;

    public PaymentCallbackService(IUnitOfWork unitOfWork, INotificationService notificationService, ILogger<PaymentCallbackService> logger)
    {
        this.unitOfWork = unitOfWork;
        this.notificationService = notificationService;
        this.logger = logger;
    }

    public async Task PaymentStatusUpdateAsync(string orderNumber, PaymentStatusCode status, string transactionId, string message, string bankRRN = "")
    {
        try
        {

            bool isRecharge = orderNumber.StartsWith("RC");
            TransactionRequesterDto? requesterDto = isRecharge ?
                                                        await ProcessRechargeStatus(orderNumber, status, transactionId) :
                                                        await ProcessOrderStatus(orderNumber, status, transactionId, message, bankRRN);
            if (requesterDto is not null)
            {
                QrStatusDto statusDto = new QrStatusDto(orderNumber, status, message, requesterDto.PaymentId);

                var machine = await unitOfWork.Machines.GetMachineAsync(requesterDto.MachineId);
                string connectionId = machine?.ConnectionId ?? string.Empty;

                if (!string.IsNullOrEmpty(connectionId))
                    await notificationService.PaymentStatusToMachineAsync(statusDto, connectionId);
                else
                {
                    if (requesterDto.VendorId > 0)
                        await notificationService.PaymentStatusToAllMachineAsync(requesterDto.VendorId, statusDto);
                    else
                        logger.LogWarning($"Vendor Id not for the order {orderNumber}");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
    }

    private async Task<TransactionRequesterDto?> ProcessOrderStatus(string orderNumber, PaymentStatusCode status, string transactionId, string message, string bankRRN = "")
    {
        try
        {
            var requester = await unitOfWork.Payments.UpdatePaymentStatus(orderNumber, status.ToString(), transactionId, message, bankRRN);
            return requester;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
            return null;
        }
    }

    private async Task<TransactionRequesterDto?> ProcessRechargeStatus(string rechargeId, PaymentStatusCode status, string transactionId)
    {
        try
        {
            return await unitOfWork.Recharges.UpdateRechargeStatus(rechargeId, status.ToString(), transactionId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
            return null;
        }
    }

}

public record TransactionRequesterDto(int VendorId, string MachineId, string PaymentId);
