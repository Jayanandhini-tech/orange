using Microsoft.Extensions.Logging;
using Splash;
using VM.Dtos;
using VM.Services.Interfaces;

namespace VM.Services;

public class FaceDeviceService : IFaceDeviceService
{
    private readonly string ipAddress;
    private readonly ILogger<FaceDeviceService> logger;
    FaceId client = new FaceId();

    public FaceDeviceService(PaymentConfig paymentConfig, ILogger<FaceDeviceService> logger)
    {
        ipAddress = paymentConfig.FaceDeviceConfig.IpAddress;
        this.logger = logger;
    }

    public async Task<bool> ConnectDevice()
    {
        try
        {
            client = new FaceId();
            client.SendTimeout = 1000;
            client.ReceiveTimeout = 1000;
            //var status = client.AsyncConnect(ipAddress, 9922, 2000);
            //await Task.CompletedTask;

            await client.ConnectAsync(ipAddress, 9922);

            FaceId_ErrorCode errorCode = client.Execute("DetectDevice()", out string answer);
            if (errorCode != FaceId_ErrorCode.Success)
            {
                logger.LogWarning(answer);
                logger.LogWarning($"Face Device Detected failed. IP Address : {ipAddress}");
                return false;
            }

            logger.LogInformation("Device connected successfully");
            return client.Connected;
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            return false;
        }

    }



    public void DisconnectDevice()
    {
        try
        {
            if (client.Connected)
                client.Close();
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);

        }
    }


    public void SetDateTime()
    {
        string cmd = $"SetDateTime(date=\"{DateTime.Now.ToString("yyyy-MM-dd")}\" time=\"{DateTime.Now.ToString("HH:mm:ss")}\")";
        FaceId_ErrorCode errorCode = client.Execute(cmd, out string answer);
        if (errorCode != FaceId_ErrorCode.Success)
            logger.LogWarning(answer);

        logger.LogInformation("Set Date and Time is success");
    }


    public void DeleteAllRecords()
    {
        try
        {
            FaceId_ErrorCode errorCode = client.Execute("DeleteAllRecord()", out string answer);
            if (errorCode != FaceId_ErrorCode.Success)
            {
                logger.LogWarning(answer);
                logger.LogWarning("DeleteAllRecord() failed");
                return;
            }

            logger.LogInformation("All Records Deleted");
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    public string[] GetRecords()
    {
        try
        {
            FaceId_ErrorCode errorCode = client.Execute("GetRecord()", out string answer);
            if (errorCode != FaceId_ErrorCode.Success)
                return [];

            string[] records = answer.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            return records;
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            return [];
        }
    }

    public FaceRecord? GetLastRecord()
    {
        try
        {
            FaceId_ErrorCode errorCode = client.Execute($"GetRecord(start_time=\"{DateTime.Now.AddSeconds(-10).ToString("yyyy-MM-dd HH:mm:ss")}\")", out string answer);
            if (errorCode != FaceId_ErrorCode.Success)
                return null;

            string[] records = answer.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            if (records.Length < 3)
                return null;

            string lastRecord = records[records.Length - 2];

            logger.LogInformation($"Answer : {answer}");
            logger.LogInformation($"Last Record : {lastRecord}");

            string? id = FaceId_Item.GetKeyValue(answer, "id");
            string? name = FaceId_Item.GetKeyValue(answer, "name");

            return new FaceRecord(id ?? "", name ?? "");
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            return null;
        }
    }

    public string GetUserImage(string Id)
    {
        try
        {
            FaceId_ErrorCode errorCode = client.Execute($"GetEmployeePhoto(id=\"{Id}\")", out string answer);
            if (errorCode != FaceId_ErrorCode.Success)
                return string.Empty;

            return FaceId_Item.GetKeyValue(answer, "photo");
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            return string.Empty;
        }
    }

}


public record FaceRecord(string Id, string Name);