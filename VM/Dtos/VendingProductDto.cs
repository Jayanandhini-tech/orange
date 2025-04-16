using System.ComponentModel;

namespace VM.Dtos;

public class VendingProductDto : INotifyPropertyChanged
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ImgPath { get; set; } = string.Empty;
    public double Price { get; set; }
    public int Qty { get; set; }
    public int Vend { get; set; }

    public double VendAmount
    {
        get
        {
            return Price * Vend;
        }
    }

    private string _status = string.Empty;
    public string Status
    {
        get { return _status; }
        set
        {
            _status = value;
            OnPropertyChanged("Status");
        }
    }

    public List<VendQtyStatusDto> VendQtyStatus { get; set; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}


public class VendQtyStatusDto : INotifyPropertyChanged
{

    private int _value = 0;
    public string _icon = "DotsHorizontal";
    public bool _processing = false;


    public bool Processing
    {
        get { return _processing; }
        set { _processing = value; OnPropertyChanged("Processing"); }
    }

    public int ProcessValue
    {
        get { return _value; }
        set { _value = value; OnPropertyChanged("ProcessValue"); }
    }

    public string StatusIcon
    {
        get { return _icon; }
        set { _icon = value; OnPropertyChanged("StatusIcon"); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
