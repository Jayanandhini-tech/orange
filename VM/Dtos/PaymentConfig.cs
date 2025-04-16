namespace VM.Dtos;
public class PaymentConfig
{
    public bool Cash { get; set; }
    public bool Upi { get; set; }
    public bool Account { get; set; }
    public bool Card { get; set; }
    public bool Counter { get; set; }
    public AccountConfig AccountConfig { get; set; } = new();
    public FaceDeviceConfig FaceDeviceConfig { get; set; } = new();
}


public class AccountConfig()
{
    public string Plan { get; set; } = string.Empty;
    public string AuthType { get; set; } = string.Empty;
    public double DailyLimit { get; set; }
    public double MonthlyLimit { get; set; }
};



public class FaceDeviceConfig()
{
    public string IpAddress { get; set; } = string.Empty;
}