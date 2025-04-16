using CMS.API.Domains;
using CMS.API.Dtos;
using CMS.API.Repository.IRepository;
using CMS.API.Services;
using CMS.API.Services.Interfaces;
using CMS.Dto;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IEmailService emailService;
    private readonly ITenant tenant;
    private readonly IUnitOfWork unitOfWork;
    private readonly INotificationService notificationService;
    private readonly ILogger<ReportsController> logger;

    public ReportsController(IEmailService emailService, ITenant tenant, IUnitOfWork unitOfWork, INotificationService notificationService, ILogger<ReportsController> logger)
    {
        this.emailService = emailService;
        this.tenant = tenant;
        this.unitOfWork = unitOfWork;
        this.notificationService = notificationService;
        this.logger = logger;
    }

    [HttpPost("email")]
    public async Task<IActionResult> EmailReport([FromForm] EmailReportDto dto)
    {
        try
        {

            var emailAddress = await unitOfWork.EmailAddresses.GetAllAsync();

            if (emailAddress.Count == 0)
                return Ok(new SuccessDto("To address not found"));

            var emaillist = emailAddress.SelectMany(x => new[]
                                  {
                                x.Address1 ?? string.Empty,
                                x.Address2 ?? string.Empty,
                                x.Address3 ?? string.Empty,
                                x.Address4 ?? string.Empty,
                                x.Address5 ?? string.Empty,
                            }).ToList();
            var Tolist = emaillist.Where(x => !string.IsNullOrEmpty(x.Trim())).Distinct().ToList();

            if (Tolist is not null && Tolist.Count > 0)
            {

                string pathToSave = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "reports", "emails");
                if (!Directory.Exists(pathToSave))
                    Directory.CreateDirectory(pathToSave);

                string fullPath = Path.Combine(pathToSave, dto.File.FileName);
                using FileStream stream = new(fullPath, FileMode.Create);
                await dto.File.CopyToAsync(stream);
                stream.Flush();
                stream.Close();

                var vendor = await unitOfWork.Vendors.GetAsync(x => x.Id == tenant.VendorId);

                string subject = $"{dto.ReportType} from {tenant.AppType} of {vendor?.ShortName}{tenant.MachineNumber}";
                string body =
                   $"""
                    Dear {vendor?.Name ?? "User"},

                        Please find the report in the attachment. 

                    Type    : {tenant.AppType},
                    Name    : {tenant.MachineName},
                    Number  : {tenant.MachineNumber}, 
                    Start   : {dto.StartDate.ToString("dd-MMM-yyyy hh:mm:ss tt")},
                    End     : {dto.EndDate.ToString("dd-MMM-yyyy hh:mm:ss tt")},

                    Thanks & Regards
                    Bharath Vending Corporation, 
                    Ph : +91 80566 80266.
                    """;

                bool success = await emailService.SendEmail(Tolist, subject, body, fullPath);
                string msg = success ? $"Mail send successfully to {string.Join(",", Tolist)}." : "Mail send failed";

                return Ok(new SuccessDto(msg));
            }
            else
            {
                return Ok(new SuccessDto("To address not found"));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
            return Ok(new SuccessDto(ex.Message));
        }
    }


    [HttpGet("live/machine/request/{machineId}")]
    public async Task<IActionResult> MachineLiveReportList(string machineId)
    {
        try
        {
            bool isMachineOwner = await unitOfWork.Machines.MachineBelongsToVendor(machineId);

            if (!isMachineOwner)
                return BadRequest("Machine not found");

            var requests = await unitOfWork.ReportRequests.GetAllMachineRequestsAsync(machineId);

            var oldRequests = requests.Where(x => x.RequestedOn < DateTime.Now.AddDays(-3)).ToList() ?? [];

            if (oldRequests.Any())
            {
                unitOfWork.ReportRequests.RemoveRange(oldRequests);
                await unitOfWork.SaveChangesAsync();
            }


            var results = requests.Where(x => x.RequestedOn >= DateTime.Now.AddDays(-3)).OrderByDescending(x => x.RequestedOn).ToList() ?? [];

            return Ok(results.Adapt<List<ReportRequestDisplayDto>>());
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("live/machine/reports/download/{Id}")]
    public async Task<IActionResult> DownloadMachineLiveReport(string id)
    {
        try
        {
            var request = await unitOfWork.ReportRequests.GetRequestByIdAsync(id);
            if (request is null)
                return BadRequest("Invalid Request Id");

            var file = System.IO.File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, request.FilePath));

            return File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "LiveReport.xls");
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }


    [HttpPost("live/machine/request")]
    public async Task<IActionResult> MachineLiveReportRequest(ReportRangeDto reportRangeDto)
    {
        try
        {
            var machine = await unitOfWork.Machines.GetAsync(x => x.Id == reportRangeDto.MachineId);
            if (machine is null)
                return BadRequest("Machine not found");

            string id = $"rr_{Guid.NewGuid().ToString("N").ToUpper()}";
            bool isSend = false;
            if (!string.IsNullOrEmpty(machine.ConnectionId))
            {
                var message = new ReportRequestDto(id, reportRangeDto.From, reportRangeDto.To);
                await notificationService.SendToMachineAsync(machine.ConnectionId, HubMethods.ReportRequest, message);
                isSend = true;
            }

            ReportRequest request = new ReportRequest()
            {
                Id = id,
                RequestedOn = DateTime.Now,
                MachineId = reportRangeDto.MachineId,
                From = reportRangeDto.From,
                To = reportRangeDto.To,
                FilePath = string.Empty,
                Sent = isSend,
                Received = false,
            };

            await unitOfWork.ReportRequests.AddAsync(request);
            await unitOfWork.SaveChangesAsync();

            string ret_message = isSend ? "Request sent successfully" : "Machine was offline";
            return Ok(new SuccessDto(ret_message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPost("live/machine/response")]
    public async Task<IActionResult> MachineLiveReportResponse([FromForm] LiveReportResponseDto dto)
    {
        try
        {
            string pathToSave = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "reports", "live");
            if (!Directory.Exists(pathToSave))
                Directory.CreateDirectory(pathToSave);

            string fullPath = Path.Combine(pathToSave, dto.File.FileName);
            using FileStream stream = new(fullPath, FileMode.Create);
            await dto.File.CopyToAsync(stream);
            stream.Flush();
            stream.Close();

            if (string.IsNullOrEmpty(dto.RequestId))
                return Ok("Request Id not found");

            var rep_request = await unitOfWork.ReportRequests.GetRequestByIdAsync(dto.RequestId);
            if (rep_request is null)
                return Ok("Request Id not found");

            rep_request.Received = true;
            rep_request.FilePath = $"reports/live/{dto.File.FileName}";

            await unitOfWork.SaveChangesAsync();

            await notificationService.SendToDashboard(rep_request.VendorId, HubMethods.ReportResponse, new ReportReceivedAlertDto(rep_request.Id, $"Report received from {tenant.MachineName}"));

            return Ok(new SuccessDto("Successfully updated"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
