using CMS.Dto;
using ESC_POS_USB_NET.Printer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Speech.Synthesis;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using VM.Domains;
using VM.Dtos;
using VM.Extensions;
using VM.Services.Interfaces;

namespace VM.Pages;


public partial class PrintBillPage : Page
{
    private readonly AppDbContext dbContext;
    private readonly PaymentConfig paymentConfig;
    private readonly IServiceProvider serviceProvider;
    private readonly SpeechSynthesizer speech;
    private readonly ILogger<PrintBillPage> logger;

    DispatcherTimer tmr = new DispatcherTimer();
    int timer_count = 0;

    public PrintBillPage(AppDbContext dbContext, PaymentConfig paymentConfig, IServiceProvider serviceProvider, SpeechSynthesizer speech, ILogger<PrintBillPage> logger)
    {
        InitializeComponent();
        this.dbContext = dbContext;
        this.paymentConfig = paymentConfig;
        this.serviceProvider = serviceProvider;
        this.speech = speech;
        this.logger = logger;
    }

    private async void PrintBillPage_OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            tmr.Interval = TimeSpan.FromSeconds(1);
            tmr.Tick += Tmr_Tick;


            var bill = await GetBillDataFromLocal(DataStore.orderNumber);
            // var bill = await GetBillDataFromLocal("BVC01K000010");
            if (bill is not null)
            {
                CtlBill.ItemsSource = new List<BillPrintDto>() { bill };

                if (DataStore.AppType == StrDir.AppType.KIOSK)
                {
                    var categoryToken = DataStore.CategoryWiseToken ? await GetProductByCategory(DataStore.orderNumber) : new List<CategoryWiseTokenDto>();
                    PrintInvoice(bill, categoryToken);
                }

                if (DataStore.AppType == StrDir.AppType.VM)
                {
                    lblMessage.Text = "Thank you for your purchase! \r\nWant a bill? Just snap a pic of the screen!";
                }
            }

            var eventService = serviceProvider.GetRequiredService<IEventService>();
            eventService.RaiseOrderCompleteEvent(DataStore.orderNumber);

            tmr.Start();
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    public async Task<BillPrintDto?> GetBillDataFromLocal(string orderNumber)
    {
        try
        {
            var order = await dbContext.Orders.Include(x => x.Items).FirstOrDefaultAsync(x => x.OrderNumber == orderNumber);
            if (order is not null)
            {

                var items = order.Items!.Select(x => new Items(x.ProductName, x.Rate, x.Gst, x.Price, x.VendQty)).ToList();

                var gst = items.GroupBy(i => i.Gst).Select(g =>
                                    new GstDto()
                                    {
                                        GstSlab = g.Key,
                                        TaxableAmount = g.Sum(x => x.TotalRate),
                                        TotalGst = g.Sum(x => x.TotalRate) * (g.Key / 100.0),
                                    }).OrderBy(x => x.GstSlab).ToList();

                double beforeRound = Math.Round(gst.Sum(x => x.TaxableAmount) + gst.Sum(x => x.TotalGst));
                double roundoff = Math.Round(order.Total - beforeRound, 2);

                Calculation calculation = new Calculation(gst.Sum(x => x.TaxableAmount), gst.Sum(x => x.Cgst), gst.Sum(x => x.Sgst), roundoff, order.Total);

                var company = await dbContext.Company.OrderByDescending(x => x.Id).FirstOrDefaultAsync();

                var billPrintDto = new BillPrintDto(
                                        new CMS.Dto.Company(company!.Name, company.Address, company.Phone, company.GstNumber),
                                        new BillHeader(order.OrderNumber, order.OrderDate, $"{DataStore.AppType[0]} -{DataStore.MachineInfo.MachineNumber}", order.DeliveryType, order.Id),
                                        items,
                                        calculation,
                                        new PaymentDto(order.PaymentType, "", order.PaidAmount, order.RefundedAmount),
                                        gst);
                return billPrintDto;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
        return null;
    }

    public async Task<List<CategoryWiseTokenDto>> GetProductByCategory(string orderNumber)
    {
        try
        {
            var order = await dbContext.Orders.Include(x => x.Items).FirstOrDefaultAsync(x => x.OrderNumber == orderNumber);

            if (order is null)
                return [];

            var items = order.Items ?? [];

            var productIds = items.Select(x => x.ProductId).Distinct().ToList();

            var dbProducts = await dbContext.Products.Include(x => x.Category).Where(x => productIds.Contains(x.Id)).ToListAsync();

            var result = (from i in items
                          join p in dbProducts on i.ProductId equals p.Id
                          select new
                          {
                              Category = p.Category?.Name ?? p.CategoryId,
                              i.ProductId,
                              i.ProductName,
                              i.VendQty
                          }).ToList();

            var res2 = (from r in result
                        group r by r.Category into g
                        select new CategoryWiseTokenDto
                        {
                            Category = g.Key,
                            Products = g.Select(x => new TokenProductDto(x.ProductId, x.ProductName, x.VendQty)).ToList()
                        }).ToList();

            return res2;
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            return [];
        }

    }


    private void PrintInvoice(BillPrintDto bill, List<CategoryWiseTokenDto> categoryWiseTokens)
    {
        try
        {

            if (bill is null)
                return;
            //    TVS-E RP 3230
            Printer printer = new Printer(DataStore.PrinterName, "UTF-8");
            printer.InitializePrint();
            printer.AlignCenter();
            printer.BoldMode(bill.Company.Name);
            printer.NewLine();
            printer.Append(bill.Company.Address);
            printer.Append($"Ph : {bill.Company.Mobile}");
            printer.Append($"GSTIN : {bill.Company.GstIn}");
            printer.NewLines(1);
            printer.Separator();
            printer.NewLines(1);

            printer.AlignLeft();


            printer.Append($"{"Bill No".PadRight(10)} : {bill.BillHeader.BillNo.PadRight(14)}");
            printer.Append($"{"Date".PadRight(10)} : {bill.BillHeader.Date.ToString("dd-MM-yyyy").PadRight(14)} {"Time".PadRight(8)} : {bill.BillHeader.Date.ToString("hh:mm tt")}");
            printer.Append($"{"Billed By".PadRight(10)} : {bill.BillHeader.Billedby.PadRight(14)} {"Delivery".PadRight(8)} : {bill.BillHeader.DeliveryType}");
            printer.Separator();

            string header = $"{"ITEM".PadRight(27)}{"GST".PadCenter(4)}{"PRICE".PadCenter(6)}{"QTY".PadCenter(4)}{"AMOUNT".PadLeft(7)}";
            printer.Append(header);
            printer.Separator();

            //  printer.SetLineHeight((byte)80);
            foreach (var item in bill.Items)
            {
                string txt = $"{item.Name.PadRight(27)}{(item.Gst + "%").PadCenter(4)}{item.Price.ToString("0.00").PadLeft(6)}{item.Qty.ToString().PadCenter(4)}{item.TotalPrice.ToString("0.00").PadLeft(7)}";
                printer.Append(txt);
            }

            printer.InitializePrint();
            printer.Separator();
            printer.AlignRight();
            printer.DoubleWidth2();
            printer.BoldMode($"TOTAL Rs. {bill.Calculation.Total.ToString("0.00")}");
            printer.NormalWidth();
            printer.AlignLeft();

            if (bill.GstTable is not null)
            {
                printer.NewLines(3);
                string tax_header = string.Format(" {0,-6}{1,10}{2, 8}{3,8}{4,12} ", "GST", "Taxable", "CGST", "SGST", "TotalGST");
                printer.Append(tax_header);

                foreach (var gst in bill.GstTable)
                {
                    string record = string.Format(" {0,-6}{1,10}{2, 8}{3,8}{4,12} ", (gst.GstSlab + "%"), gst.TaxableAmount.ToString("0.00"), gst.Cgst.ToString("0.00"), gst.Sgst.ToString("0.00"), gst.TotalGst.ToString("0.00"));
                    printer.Append(record);
                }
            }

            printer.NewLine();


            printer.Append($"Payment : {bill.Payment.Type}");
            printer.Append($"Paid : {bill.Payment.Paid}");
            printer.Append($"Refund : {bill.Payment.Refunded}");
            printer.Append(bill.Payment.Reference);
            printer.Separator();

            if (paymentConfig.Counter && DataStore.AppType == StrDir.AppType.KIOSK && bill.Payment.Type == StrDir.PaymentType.COUNTER)
            {
                printer.NewLines(2);
                printer.DoubleWidth3();
                printer.BoldMode($"{StrDir.PaymentType.COUNTER}");
                printer.NormalWidth();
                printer.NewLines(2);
            }

            printer.AlignCenter();
            printer.Append("Thank You! Visit Again");

            if (DataStore.AppType == StrDir.AppType.KIOSK)
            {
                if (!paymentConfig.Counter)
                {
                    if (categoryWiseTokens.Count > 0)
                    {
                        // Each token for every category
                        foreach (var category in categoryWiseTokens)
                        {
                            printer.PartialPaperCut();
                            printer.SetLineHeight((byte)60);
                            printer.AlignCenter();
                            printer.BoldMode(bill.Company.Name);
                            printer.Append("-------------------------------");
                            printer.BoldMode(category.Category);
                            printer.AlignLeft();
                            printer.NewLines(2);
                            printer.BoldMode($"{"Bill No".PadRight(10)} : {bill.BillHeader.BillNo}");
                            printer.Append($"{"Date".PadRight(10)} : {bill.BillHeader.Date.ToString("dd-MM-yyyy")} Time : {bill.BillHeader.Date.ToString("hh:mm tt")}");

                            printer.Separator();

                            string Tokenheader = $"{"ITEM".PadRight(37)}{"QTY".PadCenter(5)}";

                            printer.Append(Tokenheader);
                            printer.Separator();
                            printer.SetLineHeight((byte)80);

                            foreach (var item in category.Products)
                            {
                                string txt = $"{item.ProductName.PadRight(37)} {item.VendQty.ToString().PadCenter(5)}";
                                printer.Append(txt);
                            }

                            printer.SetLineHeight((byte)60);
                            printer.Separator();
                        }
                    }
                    else
                    {
                        // Single Token
                        printer.PartialPaperCut();
                        printer.SetLineHeight((byte)60);
                        printer.AlignCenter();
                        printer.BoldMode(bill.Company.Name);
                        printer.Append("-------------------------------");
                        printer.AlignLeft();
                        printer.NewLines(2);
                        printer.BoldMode($"{"Bill No".PadRight(10)} : {bill.BillHeader.BillNo}");
                        printer.Append($"{"Date".PadRight(10)} : {bill.BillHeader.Date.ToString("dd-MM-yyyy")} Time : {bill.BillHeader.Date.ToString("hh:mm tt")}");

                        printer.Separator();

                        string Tokenheader = $"{"ITEM".PadRight(37)}{"QTY".PadCenter(5)}";

                        printer.Append(Tokenheader);
                        printer.Separator();
                        printer.SetLineHeight((byte)80);

                        foreach (var item in bill.Items)
                        {
                            string txt = $"{item.Name.PadRight(37)} {item.Qty.ToString().PadCenter(5)}";
                            printer.Append(txt);
                        }

                        printer.SetLineHeight((byte)60);
                        printer.Separator();
                    }
                }
            }
            printer.FullPaperCut();
            printer.PrintDocument();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
    }

    private void Tmr_Tick(object? sender, EventArgs e)
    {
        timer_count++;
        if (timer_count > 10)
            MoveNext();
    }

    private void MoveNext()
    {
        tmr.Stop();
        tmr.Tick -= Tmr_Tick;
        speech.SpeakAsync("Thank you! Visit Again");
        NavigationService.Navigate(serviceProvider.GetRequiredService<HomePage>());
    }

    private void btnNext_Click(object sender, RoutedEventArgs e)
    {
        MoveNext();
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        DataStore.orderNumber = string.Empty;
        DataStore.selectedProducts = [];
        DataStore.deliveryType = "";
    }
}


