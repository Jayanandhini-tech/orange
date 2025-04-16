using CMS.API.Domains;
using CMS.API.Dtos;
using CMS.API.Repository.IRepository;
using CMS.API.Services.Interfaces;
using CMS.Dto;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class MachineLogsController : ControllerBase
{
    private readonly ITenant tenant;
    private readonly IUnitOfWork unitOfWork;
    private readonly INotificationService notificationService;
    private readonly ILogger<MachineLogsController> logger;

    public MachineLogsController(ITenant tenant, IUnitOfWork unitOfWork, INotificationService notificationService, ILogger<MachineLogsController> logger)
    {
        this.tenant = tenant;
        this.unitOfWork = unitOfWork;
        this.notificationService = notificationService;
        this.logger = logger;
    }



    [HttpGet("{machineId}")]
    public async Task<IActionResult> LogReportList(string machineId)
    {
        try
        {
            bool isMachineOwner = await unitOfWork.Machines.MachineBelongsToVendor(machineId);

            if (!isMachineOwner)
                return BadRequest("Machine not found");

            var requests = await unitOfWork.LogRequests.GetAllAsync(x => x.MachineId == machineId);

            var oldRequests = requests.Where(x => x.RequestedOn < DateTime.Now.AddDays(-3)).ToList() ?? [];

            if (oldRequests.Any())
            {
                unitOfWork.LogRequests.RemoveRange(oldRequests);
                await unitOfWork.SaveChangesAsync();
            }


            var results = requests.Where(x => x.RequestedOn >= DateTime.Now.AddDays(-3)).OrderByDescending(x => x.RequestedOn).ToList() ?? [];

            return Ok(results.Adapt<List<LogRequestDisplayDto>>());
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("download/{Id}")]
    public async Task<IActionResult> DownloadLogFile(string id)
    {
        try
        {
            var request = await unitOfWork.LogRequests.GetAsync(x => x.Id == id);
            if (request is null)
                return BadRequest("Invalid Request Id");

            var file = System.IO.File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, request.FilePath));

            return File(file, "text/plain", $"{request.LogDate.ToString("yyyy-MM-dd")}.log");
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }


    [HttpPost("request")]
    public async Task<IActionResult> SendLogRequestToMachine(LogRequestDto logRequestDto)
    {
        try
        {
            var machine = await unitOfWork.Machines.GetAsync(x => x.Id == logRequestDto.MachineId);
            if (machine is null)
                return BadRequest("Machine not found");

            string id = $"lg_{Guid.NewGuid().ToString("N").ToUpper()}";
            bool isSend = false;
            if (!string.IsNullOrEmpty(machine.ConnectionId))
            {
                var message = new LogRequestNotificationDto(id, logRequestDto.LogDate);
                await notificationService.SendToMachineAsync(machine.ConnectionId, HubMethods.LogRequest, message);
                isSend = true;
            }

            LogRequest logRequest = new LogRequest()
            {
                Id = id,
                RequestedOn = DateTime.Now,
                MachineId = logRequestDto.MachineId,
                LogDate = logRequestDto.LogDate,
                Sent = isSend
            };

            await unitOfWork.LogRequests.AddAsync(logRequest);
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

    [HttpPost("response")]
    public async Task<IActionResult> MachineLiveReportResponse([FromForm] LogResponseDto dto)
    {
        try
        {
            if (string.IsNullOrEmpty(dto.RequestId))
                return Ok("Request Id not found");

            var logRequest = await unitOfWork.LogRequests.GetAsync(x => x.Id == dto.RequestId, tracked: true);
            if (logRequest is null)
                return Ok("Request Id not found");

            string filePath = string.Empty;
            if (dto.Success && dto.File is not null)
            {
                string pathToSave = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "machinelogs", tenant.MachineName.ToLower());
                if (!Directory.Exists(pathToSave))
                    Directory.CreateDirectory(pathToSave);

                string fullPath = Path.Combine(pathToSave, dto.File.FileName);
                using FileStream stream = new(fullPath, FileMode.Create);
                await dto.File.CopyToAsync(stream);
                stream.Flush();
                stream.Close();
                filePath = $"machinelogs/{tenant.MachineName.ToLower()}/{dto.File.FileName}";
            }



            logRequest.Sent = true;
            logRequest.Received = true;
            logRequest.Success = dto.Success;
            logRequest.Message = dto.Message ?? "";
            logRequest.FilePath = filePath;

            await unitOfWork.SaveChangesAsync();

            await notificationService.SendToDashboard(logRequest.VendorId, HubMethods.LogResponse, new LogReceivedAlertDto(logRequest.Id, $"Log received from {tenant.MachineName}"));

            return Ok(new SuccessDto("Successfully updated"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

}
