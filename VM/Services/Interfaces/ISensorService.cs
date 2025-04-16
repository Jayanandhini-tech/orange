using System.IO.Ports;

namespace VM.Services.Interfaces;

public interface ISensorService
{
    //void CalibrateAllSensorInstantly();
    void CalibrateAllSensorPermanantly();
    bool Close();
    void Init(string portName, int baudRate = 115200, int databits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One);
    bool IsOpen();
    bool Open();
    //void ReCalibrateInstant(string port);
    void ReCalibratePermanant(string port);
}