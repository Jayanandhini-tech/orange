using System.ComponentModel;

namespace VM.Dtos;

public class AllMotorTestDto : INotifyPropertyChanged
{
    private int statusCode;
    private string status = string.Empty;
    private bool success;

    public int MotorNumber { get; set; }
    public int StatusCode
    {
        get => statusCode;
        set
        {
            statusCode = value;
            OnPropertyChanged("StatusCode");
        }
    }
    public string Status
    {
        get => status;
        set
        {
            status = value;
            OnPropertyChanged("Status");
        }
    }

    public bool Success
    {
        get => success; set
        {
            success = value;
            OnPropertyChanged("StatusCode");
            OnPropertyChanged("Status");
            OnPropertyChanged("Success");
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

