namespace CMS.Dto;


public record BillPrintDto(Company Company, BillHeader BillHeader, List<Items> Items, Calculation Calculation, PaymentDto Payment, List<GstDto>? GstTable = null);
public record Company(string Name, string Address, string Mobile, string GstIn);
public record BillHeader(string BillNo, DateTime Date, string Billedby, string DeliveryType, string OrderId);
public record Calculation(double SubTotal, double Cgst, double Sgst, double Roundoff, double Total);
public record PaymentDto(string Type, string Reference, double Paid, double Refunded);

 

public class Items(string Name, double Rate, int Gst, double Price, int Qty)
{
    public string Name { get; } = Name;
    public double Rate { get; } = Rate;
    public int Gst { get; } = Gst;
     public double Price { get; } = Price;
    public int Qty { get; } = Qty;
    public double TotalRate { get { return Rate * Qty; } }
    public double TotalPrice { get { return Price * Qty; } }
}
