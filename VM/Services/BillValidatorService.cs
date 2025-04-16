using Microsoft.Extensions.Logging;
using System.IO.Ports;
using System.Text;
using VM.Services.Interfaces;

namespace VM.Services;

public class BillValidatorService : IBillValidatorService
{
    private readonly ILogger<BillValidatorService> logger;
    private SerialPort sp;
    private Dictionary<int, int> Bills = new Dictionary<int, int>();

    private int RequestAmount = 0;
    private int MaxLimit = 0;
    private int NoteValue = 0;
    private int ReceivedAmount = 0;


    public event Action<string>? OnMessageRaised;
    public event Action<Dictionary<int, int>>? OnAmountReceived;

    public BillValidatorService(ILogger<BillValidatorService> logger)
    {
        this.logger = logger;

        sp = new SerialPort("COM2", 9600, Parity.Even, 8, StopBits.One);
        sp.DataReceived += Sp_DataReceived;
        ResetBills();
    }

    public bool Open()
    {
        try
        {
            if (!sp.IsOpen)
                sp.Open();

            return sp.IsOpen;
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            return false;
        }
    }

    public bool IsOpen()
    {
        return sp.IsOpen;
    }

    public bool Close()
    {
        if (sp.IsOpen)
            sp.Close();

        return sp.IsOpen;
    }

    public bool EnableValidator()
    {
        try
        {
            if (!Open())
                return false;

            return WriteData("02") && WriteData("3E");
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            return false;
        }
    }

    public bool DisableValidator()
    {
        try
        {
            if (sp.IsOpen)
                WriteData("5E");

            return Close();
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            return false;
        }
    }

    public bool InitializeAmountRequest(int requestAmount, int maxLimit = 0)
    {
        try
        {
            if (maxLimit == 0)
                maxLimit = requestAmount;

            RequestAmount = requestAmount;
            MaxLimit = maxLimit;
            ReceivedAmount = 0;
            NoteValue = 0;
            ResetBills();

            return EnableValidator();
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            return false;
        }
    }

    private void Sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        try
        {
            int length = sp.BytesToRead;
            byte[] buf = new byte[length];
            sp.Read(buf, 0, length);
            string response = ByteArrayToString(buf);
            response = response.Trim().ToUpper();
            logger.LogInformation($"<< {response}");
            ProcessResponse(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    private void ProcessResponse(string response)
    {
        try
        {
            switch (response)
            {
                case "20":
                    logger.LogInformation("Motor Failure");
                    break;
                case "21":
                    logger.LogInformation("CheckSum Error");
                    break;
                case "22":
                    logger.LogInformation("Cash Jam");
                    break;
                case "23":
                    logger.LogInformation("Cash Remove");
                    break;
                case "24":
                    logger.LogInformation("Stacker Open");
                    break;
                case "25":
                    logger.LogInformation("Sensor Problem");
                    break;
                case "27":
                    logger.LogInformation("Bill Fish");
                    WriteData("5E");
                    WriteData("3E");
                    break;
                case "28":
                    logger.LogInformation("Stacker Problem");
                    break;
                case "29":
                    // acc.writelog("Bill validator -> Bill Reject");
                    // wriredb("Bill validator -> Bill Reject");
                    break;
                case "808F":
                    WriteData("02");
                    break;
                case "8F":
                    WriteData("02");
                    break;
                case "8141":
                case "41":
                    WriteData(CheckEnough(10));
                    break;
                case "8142":
                case "42":
                    WriteData(CheckEnough(20));
                    break;
                case "8143":
                case "43":
                    WriteData(CheckEnough(50));
                    break;
                case "8144":
                case "44":
                    WriteData(CheckEnough(100));
                    break;
                case "8145":
                case "45":
                    WriteData(CheckEnough(200));
                    break;
                case "10":
                    ConfirmAmountReceived();
                    break;
                case "11":
                    NoteValue = 0;
                    break;
                case "02":
                case "5E":
                case "5e":
                case "3E":
                case "3e":
                    break;
                default:
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    private bool WriteData(string cmd)
    {
        logger.LogInformation($">> {cmd}");
        byte[] bytesToSend = StrToByteArray(cmd.ToUpper());
        sp.Write(bytesToSend, 0, bytesToSend.Length);
        return true;
    }

    private byte[] StrToByteArray(string str)
    {
        Dictionary<string, byte> hexindex = new Dictionary<string, byte>();
        for (int i = 0; i <= 255; i++)
            hexindex.Add(i.ToString("X2"), (byte)i);

        List<byte> hexres = new List<byte>();
        for (int i = 0; i < str.Length; i += 2)
            hexres.Add(hexindex[str.Substring(i, 2)]);

        return hexres.ToArray();
    }

    private string ByteArrayToString(byte[] ba)
    {
        StringBuilder hex = new StringBuilder(ba.Length * 2);
        foreach (byte b in ba)
            hex.AppendFormat("{0:x2}", b);
        return hex.ToString();
    }

    private void ResetBills()
    {
        Bills = new()
        {
            {10,0 },
            {20,0 },
            {50,0 },
            {100,0},
            {200,0},
            {500,0}
        };
    }

    private string CheckEnough(int val)
    {
        if (RequestAmount == 0 || (ReceivedAmount + val) <= MaxLimit)
        {
            NoteValue = val;
            return "02";
        }

        NoteValue = 0;
        OnMessageRaised?.Invoke($"Please insert less than {val}. Maximum accepted amount Rs. {MaxLimit}");
        return "0F";
    }

    private void ConfirmAmountReceived()
    {
        if (NoteValue == 0)
            return;

        if (Bills.ContainsKey(NoteValue))
        {
            Bills[NoteValue] = Bills[NoteValue] + 1;
            ReceivedAmount += NoteValue;
            OnAmountReceived?.Invoke(Bills);
        }
    }
}
