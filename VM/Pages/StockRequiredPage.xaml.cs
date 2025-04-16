using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Windows;
using System.Windows.Controls;
using VM.Domains;
using VM.Dtos;

namespace VM.Pages;

public partial class StockRequiredPage : Page
{
    private readonly AppDbContext dbContext;
    private readonly ILogger<StockRequiredPage> logger;

    List<StockRequiredDto> Stocks = [];
    int recordsPerPage = 20;
    int pageNumber = 0;
    int TotalPages = 0;

    public StockRequiredPage(AppDbContext dbContext, ILogger<StockRequiredPage> logger)
    {
        InitializeComponent();
        this.dbContext = dbContext;
        this.logger = logger;

    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var trayStocks = await dbContext.MotorSettings
                              .GroupBy(x => x.ProductId)
                              .Select(g => new
                              {
                                  ProductId = g.Key,
                                  Capacity = g.Sum(x => x.Capacity),
                                  Stock = g.Sum(x => x.Stock)
                              }).ToListAsync();

            var productIds = trayStocks.Select(x => x.ProductId).ToList();
            var Products = await dbContext.Products.Where(x => productIds.Contains(x.Id)).ToListAsync();
            Stocks = (from ts in trayStocks
                      join p in Products on ts.ProductId equals p.Id
                      select new
                      {
                          p.Name,
                          ts.Capacity,
                          ts.Stock,
                          Required = ts.Capacity - ts.Stock
                      })
                      .OrderByDescending(x => x.Required)
                      .Select((r, index) =>
                      new StockRequiredDto(index + 1, r.Name, r.Capacity, r.Stock, r.Required)).ToList();



            recordsPerPage = this.ActualHeight > 1200 ? 40 : 20;

            TotalPages = Stocks.Count % recordsPerPage == 0 ? Stocks.Count / recordsPerPage : (Stocks.Count / recordsPerPage) + 1;

            ShowRecords();
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    private void btnMoveNext_Click(object sender, RoutedEventArgs e)
    {
        if (pageNumber < (TotalPages - 1))
        {
            pageNumber++;
            ShowRecords();
        }
    }

    private void btnMovePrevious_Click(object sender, RoutedEventArgs e)
    {
        if (pageNumber > 0)
        {
            pageNumber--;
            ShowRecords();
        }
    }

    private void ShowRecords()
    {
        var stocks = Stocks.Skip(pageNumber * recordsPerPage).Take(recordsPerPage).ToList();
        dgStocks.ItemsSource = stocks;

        lblPage.Text = $"{pageNumber + 1} / {TotalPages}";
    }

    private void btnBack_Click(object sender, RoutedEventArgs e)
    {
        NavigationService.GoBack();
    }


}
