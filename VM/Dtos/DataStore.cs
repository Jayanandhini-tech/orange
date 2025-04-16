using VM.Domains;

namespace VM.Dtos;

public static class DataStore
{
    public static string AppType = "VM";
    public static Machine MachineInfo = new Machine() { Id = "" };

    public static int playIndex = -1;
    public static List<string> videos = new List<string>();

    public static List<DisplayProductDto> selectedProducts = [];
    public static OrderPaymentDto PaymentDto = new OrderPaymentDto();

    public static string orderNumber = string.Empty;
    public static string deliveryType = string.Empty;

    public static string Ref_orderNumber = string.Empty;


    public static bool CategoryWiseToken = false;
    public static string PrinterName = "TVS-E RP 3230";
    public static int CabinCount = 1;
    public static bool AntiThieft = false;
    public static int OrangeCount { get; set; } = 0;
    public static bool IsVendingStarted = false;
    public static string categoryFilter { get; internal set; }
}

