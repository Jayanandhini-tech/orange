namespace VM.Services.Interfaces;

public interface IFaceDeviceService
{
    Task<bool> ConnectDevice();
    void DisconnectDevice();
    void SetDateTime();
    void DeleteAllRecords();
    FaceRecord? GetLastRecord();
    string[] GetRecords();
    string GetUserImage(string Id);
}