namespace CMS.Dto;

public class PaymentSettingsDto
{
    public bool Cash { get; set; }
    public bool Upi { get; set; }
    public bool Account { get; set; }
    public bool Card { get; set; }
    public bool Counter { get; set; }

    public PaymentAccountSettingsDto AccountSettings { get; set; } = new();
    public FaceDeviceSettingsDto FaceDeviceSettings { get; set; } = new();
}


public class PaymentAccountSettingsDto
{
    public string AccountPlan { get; set; } = string.Empty;
    public string AuthType { get; set; } = string.Empty;
    public double MonthlyLimit { get; set; }
    public double DailyLimit { get; set; }
}

public class FaceDeviceSettingsDto
{
    public string IpAddress { get; set; } = string.Empty;
}
