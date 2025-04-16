using System.ComponentModel;

namespace VM.Dtos;


public class CategoryProductDto
{
    public string CategoryId { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public string ImgPath { get; set; } = string.Empty;
    public List<DisplayProductDto> Products { get; set; } = new List<DisplayProductDto>();
}

public class DisplayProductDto : INotifyPropertyChanged
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double Price { get; set; }
    public double Rate { get; set; }
    public int Gst { get; set; }
    public string ImgPath { get; set; } = string.Empty;
    public int Stock { get; set; }
    public DateTime UpdatedOn { get; set; }


    public bool IsSelected
    {
        get { return _qty > 0; }
    }

    public bool IsEmpty
    {
        get { return _qty == 0; }
    }

    private int _qty = 0;

    public int qty
    {
        get { return _qty; }
        set { _qty = value; OnPropertyChanged("qty"); OnPropertyChanged("amount"); OnPropertyChanged("IsEmpty"); OnPropertyChanged("IsSelected"); }
    }

    public double amount
    {
        get { return _qty * Price; }
    }


    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
