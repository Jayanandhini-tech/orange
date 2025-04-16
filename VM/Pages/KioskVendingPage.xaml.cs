using CMS.Dto;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Windows;
using System.Windows.Controls;
using VM.Domains;
using VM.Dtos;
using VM.Services.Interfaces;

namespace VM.Pages;

public partial class KioskVendingPage : Page
{

    private readonly AppDbContext dbContext;
    private readonly IOrderService orderService;
    private readonly PaymentConfig paymentConfig;
    private readonly IServerClient serverClient;
    private readonly PrintBillPage printBillPage;
    private readonly ILogger<KioskVendingPage> logger;

    public KioskVendingPage(AppDbContext dbContext, IOrderService orderService,
        PaymentConfig paymentConfig, IServerClient serverClient,
        PrintBillPage printBillPage, ILogger<KioskVendingPage> logger)
    {
        InitializeComponent();


        this.dbContext = dbContext;
        this.orderService = orderService;
        this.paymentConfig = paymentConfig;
        this.serverClient = serverClient;
        this.printBillPage = printBillPage;
        this.logger = logger;
    }


    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        CreateAndSubmitOrder();
    }

    public async void CreateAndSubmitOrder()
    {
        try
        {
            var order = await CreateOrderAsync();
            if (order is not null)
                await UpdateStockAsync(order);

        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
        finally
        {
            NavigationService.Navigate(printBillPage);
        }
    }

    public async Task<Order?> CreateOrderAsync()
    {
        try
        {
            if (DataStore.orderNumber == string.Empty)
            {
                string orderNumber = orderService.CreateOrderAsync(
                                            DataStore.selectedProducts,
                                             DataStore.PaymentDto,
                                            DataStore.deliveryType).GetAwaiter().GetResult();
                DataStore.orderNumber = orderNumber;
            }

            var order = dbContext.Orders.Include(x => x.Items).FirstOrDefault(x => x.OrderNumber == DataStore.orderNumber);

            if (order is not null)
            {
                foreach (var item in order.Items!)
                {
                    item.VendQty = item.Qty;
                }

                order.Total = order.PaidAmount;
                order.IsViewed = false;
                order.Status = StrDir.OrderStatus.SUCCESS;

                if (paymentConfig.Counter)
                {
                    order.IsPaid = false;
                    order.PaidAmount = 0;
                }


                await dbContext.SaveChangesAsync();
            }

            return order;
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            return null;
        }
    }

    public async Task UpdateStockAsync(Order order)
    {
        try
        {
            var orderDespatchDto = new OrderDespatchDto()
            {
                OrderNumber = order.OrderNumber,
                AppUserId = order.UserId,
                DespatchItems = order.Items!.Select(x => new DespatchItemDto(x.ProductId, x.VendQty, x.UpdatedOn)).ToList(),
            };

            var response = await serverClient.PostAsync("api/order/kiosk/stockupdate", orderDespatchDto);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                string respText = await response.Content.ReadAsStringAsync();
                logger.LogInformation($"Order stock update failed. Status Code : {response.StatusCode}, Response : {respText}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }



}
