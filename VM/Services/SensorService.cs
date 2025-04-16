using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IO.Ports;
using VM.Services.Interfaces;

namespace VM.Services;

public class SensorService : ISensorService
{

    private SerialPort sp = new SerialPort();
    private readonly ILogger<SensorService> logger;

    private int throughole = 150;

    public SensorService(IConfiguration configuration, ILogger<SensorService> logger)
    {

        this.logger = logger;
        throughole = configuration.GetValue<int>("SensorThroughole", 150);
    }

    public void Init(string portName, int baudRate = 115200, int databits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One)
    {
        if (sp.IsOpen)
            sp.Close();

        sp.PortName = portName;
        sp.BaudRate = baudRate;
        sp.DataBits = databits;
        sp.Parity = parity;
        sp.StopBits = stopBits;
    }

    public bool Open()
    {
        if (sp.IsOpen)
            return true;
        try
        {
            sp.Open();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            return false;
        }
    }

    public bool Close()
    {
        sp.Close();
        return true;
    }

    public bool IsOpen()
    {
        return sp.IsOpen;
    }

    public void CalibrateAllSensorPermanantly()
    {
        if (SerialPort.GetPortNames().Any(x => x == "COM5"))
            ReCalibratePermanant("COM5");


        if (SerialPort.GetPortNames().Any(x => x == "COM6"))
            ReCalibratePermanant("COM6");
    }

    public void CalibrateAllSensorInstantly()
    {
        if (SerialPort.GetPortNames().Any(x => x == "COM5"))
            ReCalibrateInstant("COM5");


        if (SerialPort.GetPortNames().Any(x => x == "COM6"))
            ReCalibrateInstant("COM6");
    }

    public void ReCalibratePermanant(string port)
    {
        Init(port);
        if (Open())
        {
            sp.WriteLine("$"); // Send stop command to any data araivel .
            sp.DiscardInBuffer(); // Clear existing data
            sp.WriteLine("@");  // This command send data continuesly
            string sensorValue = sp.ReadTo("*"); // Read data upto * 
            Thread.Sleep(10);
            sp.WriteLine("$");

            string[] sensorArray = sensorValue.Replace("#01", "").Trim(',').Split(',', (char)StringSplitOptions.RemoveEmptyEntries);

            var sensorreadings = sensorArray.Take(20).Select(x => Convert.ToInt32(x)).ToList();

            List<string> refernce_new = new List<string>();

            foreach (var read_value in sensorreadings)
            {
                int x = read_value > throughole ? read_value - throughole : read_value - (int)(read_value * 0.2);
                refernce_new.Add(x.ToString("0000"));
            }

            string newRefernce = $"#02,{string.Join(",", refernce_new)},{sensorArray[sensorArray.Length - 1]}*";
            foreach (char c in newRefernce)
            {
                sp.Write(c.ToString());
                Thread.Sleep(1);
            }
            Thread.Sleep(10);
            sp.Close();

            logger.LogInformation($"Sensor on port {port} recalibarate with threshold of {throughole}");
        }
    }

    public void ReCalibrateInstant(string port)
    {
        Init(port);
        if (Open())
        {
            string CalibrateCmd = $"#03,{throughole.ToString("0000")}*";
            foreach (char c in CalibrateCmd)
            {
                sp.Write(c.ToString());
                Thread.Sleep(1);
            }
            sp.Close();
        }
    }



}
