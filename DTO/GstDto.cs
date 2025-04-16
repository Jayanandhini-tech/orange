namespace CMS.Dto;

public class GstDto
{
    public int GstSlab { get; set; }
    public double TaxableAmount { get; set; }
    public double TotalGst { get; set; }
    public double Sgst { get { return TotalGst / 2; } }
    public double Cgst { get { return TotalGst / 2; } }
  
}

