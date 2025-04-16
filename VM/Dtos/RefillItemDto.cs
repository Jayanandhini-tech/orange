using System.ComponentModel;

namespace VM.Dtos;

public class RefillItemDto : INotifyPropertyChanged
{
    public int CabinId { get; set; }
    public int MotorNumber { get; set; }
    public string Id { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ImgPath { get; set; } = string.Empty;
    public double Price { get; set; }
    public int Capacity { get; set; }

    private int _stock = 0;

    public int Stock
    {
        get
        {
            return _stock;
        }
        set
        {
            _stock = value;
            OnPropertyChanged("Stock");
            OnPropertyChanged("Capacity");
            OnPropertyChanged("SoldOut");
        }
    }
    public bool SoldOut { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
