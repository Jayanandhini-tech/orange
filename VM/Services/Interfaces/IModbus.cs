using System.IO.Ports;

namespace VM.Services.Interfaces;

public interface IModbus
{
    void Init(string portName, int baudRate = 19200, int databits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.Two);
    bool IsOpen();
    bool Open();
    bool Close();
    bool RunMotor(int motor, int cabinId = 1);
    int Status(int cabinId = 1);
    bool AdminButtonPressed();
    bool GetInstantValue(int RegisterNumber);
    bool CloseAntiThieftDoor(byte cabinId = 1);
    string modbusStatus { get; } //For Juice Machine
    bool ResetJuicer(); //For Juice Machine
    bool OrangeCount(byte cabinId, ushort value);
}