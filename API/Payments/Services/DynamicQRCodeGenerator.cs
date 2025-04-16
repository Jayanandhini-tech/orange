using QRCoder;

namespace CMS.API.Payments.Services;

public class DynamicQRCodeGenerator : IDynamicQRCodeGenerator
{
    QRCodeGenerator qrGenerator;
    public DynamicQRCodeGenerator()
    {
        qrGenerator = new QRCodeGenerator();
    }

    public string CreateQR(string destinationUPIId, string billerName, string payeeMCC, string TransactionId, double amt, int mode = 22)
    {
        string input = $"upi://pay?mode={mode}&pa={destinationUPIId}&pn={billerName}&mc={payeeMCC}&tr={TransactionId}&am={amt.ToString("0.00")}&cu=INR";
        return CreateQRfromUPIstring(input);
    }

    public string CreateQRfromUPIstring(string upiString)
    {
        QRCodeData qrCodeData = qrGenerator.CreateQrCode(upiString, QRCodeGenerator.ECCLevel.L);
        PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
        byte[] qrCodeAsPngByteArr = qrCode.GetGraphic(10);
        return Convert.ToBase64String(qrCodeAsPngByteArr);
    }
}

public interface IDynamicQRCodeGenerator
{
    string CreateQR(string destinationUPIId, string billerName, string payeeMCC, string TransactionId, double amt, int mode = 22);
    string CreateQRfromUPIstring(string upiString);
}