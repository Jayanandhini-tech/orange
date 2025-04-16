using System.Windows;
using System.Windows.Controls;

namespace VM.Components;


public partial class MobileNumberDialog : UserControl
{
    public MobileNumberDialog()
    {
        InitializeComponent();
    }


    private void btnNumber_Click(object sender, RoutedEventArgs e)
    {
        Button btn = (Button)sender;

        if (txtInput.MaxLength == 0 || (txtInput.MaxLength > 0 && txtInput.Text.Length < txtInput.MaxLength))
        {
            txtInput.Text = btn.Tag.ToString() switch
            {
                "DEL" => txtInput.Text.Length > 0 ? txtInput.Text.Substring(0, txtInput.Text.Length - 1) : "",
                "CE" => "",
                _ => txtInput.Text += btn.Tag,
            };
            txtInput.Focus();
            txtInput.CaretIndex = txtInput.Text.Length;
        }
    }
}
