using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using VM.Domains;
using VM.Dtos;
using VM.Services.Interfaces;

namespace VM.Components;

public partial class ProductDialog : UserControl
{

    string selectedProductId = string.Empty;
    private readonly AppDbContext dbContext;
    private readonly ISyncService syncService;
    private readonly ILogger<ProductDialog> logger;

    List<ProductSelectDto> products = new List<ProductSelectDto>();

    TextBox txtInput;

    public ProductDialog(AppDbContext dbContext, ISyncService syncService, ILogger<ProductDialog> logger)
    {
        InitializeComponent();
        this.dbContext = dbContext;
        this.syncService = syncService;
        this.logger = logger;

        txtInput = txtFilter;
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadProduct();
    }

    public async Task LoadProduct()
    {
        try
        {
            products = await dbContext.Products.OrderBy(x => x.Name).Select(x =>
                       new ProductSelectDto()
                       {
                           Id = x.Id,
                           Name = x.Name,
                           Price = x.Price
                       }).ToListAsync();

            for (int i = 0; i < products.Count; i++)
            {
                products[i].Sno = i + 1;
            }

            lvProduct.ItemsSource = products;
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(lvProduct.ItemsSource);
            view.Filter = UserFilter;
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        try
        {
            CollectionViewSource.GetDefaultView(lvProduct.ItemsSource).Refresh();
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    private bool UserFilter(object item)
    {
        if (string.IsNullOrEmpty(txtFilter.Text))
            return true;
        else
            return ((item as ProductSelectDto)?.Name.IndexOf(txtFilter.Text, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private void dgvProduct_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (lvProduct.SelectedIndex > -1)
        {
            selectedProductId = ((ProductSelectDto)lvProduct.SelectedItem).Id;
        }
    }

    private async void btnRefreshProduct_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            btnRefreshProduct.IsEnabled = false;
            btnRefreshProduct.Content = "Please wait";

            await syncService.GetCategories();
            await syncService.GetProducts(false);
            await LoadProduct();
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
        finally
        {
            btnRefreshProduct.Content = "Refresh Products";
            btnRefreshProduct.IsEnabled = true;
        }
    }

    private void btnOk_Click(object sender, RoutedEventArgs e)
    {
        DialogHost.CloseDialogCommand.Execute(selectedProductId, null);
    }

    private void btnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogHost.CloseDialogCommand.Execute(0, null);
    }


    private void btnNumber_Click(object sender, RoutedEventArgs e)
    {
        Button btn = (Button)sender;

        if (txtInput.MaxLength == 0 || (txtInput.MaxLength > 0 && txtInput.Text.Length < txtInput.MaxLength))
        {
            txtInput.Text = btn.Tag.ToString() switch
            {
                "DEL" => txtInput.Text.Length > 0 ? txtInput.Text.Substring(0, txtInput.Text.Length - 1) : "",
                _ => txtInput.Text += btn.Tag,
            };
            txtInput.Focus();
            txtInput.CaretIndex = txtInput.Text.Length;
        }
    }


}
