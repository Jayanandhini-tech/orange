using System.Windows;
using System.Windows.Controls;

namespace VM.Components;

public partial class ItemRefill : UserControl
{
    public ItemRefill()
    {
        InitializeComponent();
        //  DataContext = this;
    }


    public int MotorNumber
    {
        get { return (int)GetValue(MotorNoProperty); }
        set { SetValue(MotorNoProperty, value); }
    }

    public string ProductName
    {
        get { return (string)GetValue(ProductNameProperty); }
        set { SetValue(ProductNameProperty, value); }
    }

    public string ImgPath
    {
        get { return (string)GetValue(ImgPathProperty); }
        set { SetValue(ImgPathProperty, value); }
    }

    public double Price
    {
        get { return (double)GetValue(PriceProperty); }
        set { SetValue(PriceProperty, value); }
    }

    public int Stock
    {
        get { return (int)GetValue(StockProperty); }
        set { SetValue(StockProperty, value); }
    }

    public int Capacity
    {
        get { return (int)GetValue(CapacityProperty); }
        set { SetValue(CapacityProperty, value); }
    }

    public bool SoldOut
    {
        get { return (bool)GetValue(SoldOutProperty); }
        set { SetValue(SoldOutProperty, value); }
    }

    public event RoutedEventHandler Click
    {
        add { AddHandler(ClickEvent, value); }
        remove { RemoveHandler(ClickEvent, value); }
    }

    public static readonly RoutedEvent ClickEvent = EventManager.RegisterRoutedEvent(
   "Click", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ItemRefill));


    // Using a DependencyProperty as the backing store for MotorNo.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty MotorNoProperty =
        DependencyProperty.Register("MotorNumber", typeof(int), typeof(ItemRefill), new PropertyMetadata(0));

    public static readonly DependencyProperty ProductNameProperty =
        DependencyProperty.Register("ProductName", typeof(string), typeof(ItemRefill), new PropertyMetadata(""));

    public static readonly DependencyProperty ImgPathProperty =
        DependencyProperty.Register("ImgPath", typeof(string), typeof(ItemRefill), new PropertyMetadata(""));

    public static readonly DependencyProperty PriceProperty =
        DependencyProperty.Register("Price", typeof(double), typeof(ItemRefill), new PropertyMetadata(0.0));

    public static readonly DependencyProperty StockProperty =
        DependencyProperty.Register("Stock", typeof(int), typeof(ItemRefill), new PropertyMetadata(0));

    public static readonly DependencyProperty CapacityProperty =
        DependencyProperty.Register("Capacity", typeof(int), typeof(ItemRefill), new PropertyMetadata(0));

    public static readonly DependencyProperty SoldOutProperty =
        DependencyProperty.Register("SoldOut", typeof(bool), typeof(ItemRefill), new PropertyMetadata(false));

    private void btn_Click(object sender, RoutedEventArgs e) => RaiseEvent(new RoutedEventArgs(ClickEvent));

}