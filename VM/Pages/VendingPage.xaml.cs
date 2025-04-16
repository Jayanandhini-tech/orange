using CMS.Dto;
using CMS.Dto.Payments.QR;
using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Windows;
using System.Windows.Controls;
using VM.Components;
using VM.Domains;
using VM.Dtos;
using VM.Services.Interfaces;

namespace VM.Pages;


public partial class VendingPage : Page
{
    private readonly AppDbContext dbContext;
    private readonly IModbus modbus;
    private readonly IOrderService orderService;
    private readonly IServerClient httpClient;
    private readonly IServiceProvider serviceProvider;
    private readonly PaymentConfig paymentConfig;
    private readonly ILogger<VendingPage> logger;

    public VendingPage(AppDbContext dbContext, IModbus modbus,
                        IOrderService orderService, IServerClient httpClient,
                        IServiceProvider serviceProvider, PaymentConfig paymentConfig,
                        ILogger<VendingPage> logger)
    {
        InitializeComponent();
        this.dbContext = dbContext;
        this.modbus = modbus;
        this.orderService = orderService;
        this.httpClient = httpClient;
        this.serviceProvider = serviceProvider;
        this.paymentConfig = paymentConfig;
        this.logger = logger;
    }

    List<VendingProductDto> vendingItems = [];

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var productIds = DataStore.selectedProducts.Select(x => x.Id).ToList();
            var productIdSortedByMotorNumber = dbContext.MotorSettings
                                                .Where(x => productIds.Contains(x.ProductId) &&
                                                        x.Stock > 0 &&
                                                        x.IsActive == true &&
                                                        x.SoldOut == false)
                                                .OrderBy(x => x.CabinId)
                                                .ThenBy(x => x.MotorNumber)
                                                .Select(x => x.ProductId)
                                                .ToList();

            vendingItems = DataStore.selectedProducts
                                .OrderBy(x => productIdSortedByMotorNumber.IndexOf(x.Id))
                                .Select(x =>
                                new VendingProductDto()
                                {
                                    Id = x.Id,
                                    Name = x.Name,
                                    ImgPath = x.ImgPath,
                                    Price = x.Price,
                                    Qty = x.qty,
                                    Vend = 0,
                                    Status = "",
                                    VendQtyStatus = Enumerable.Range(1, x.qty).Select(i => new VendQtyStatusDto()).ToList()
                                })
                                .ToList();


            StartProcessing();
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    public async void StartProcessing()
    {
        try
        {
            if (DataStore.PaymentDto.IsPaid)
            {
                if (DataStore.orderNumber == string.Empty)
                {
                    string orderNumber = await orderService.CreateOrderAsync(
                                                DataStore.selectedProducts,
                                                DataStore.PaymentDto,
                                                DataStore.deliveryType);
                    DataStore.orderNumber = orderNumber;
                }

                lstItems.ItemsSource = vendingItems;
                logger.LogInformation("Start Vending Process");
                modbus.Open();
                await StartVending();
                await CheckRefund();
            }
            else
            {
                lblMessage.Text = "Sorry order payment not received";
                logger.LogInformation("Sorry order payment not received");
                await Task.Delay(5000);
            }
            await UpdateOrder();
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
        finally
        {
            modbus.Close();
            logger.LogInformation("Vending Process Ended");
            NavigationService.Navigate(serviceProvider.GetRequiredService<PrintBillPage>());
        }
    }

    public async Task StartVending()
    {
        try
        {
            if (!modbus.IsOpen())
            {
                logger.LogInformation($"Vending Start Failed due to port closed");
                return;
            }

            foreach (var vendingItem in vendingItems)
            {
                for (int i = 0; i < vendingItem.Qty; i++)
                {
                    try
                    {

                        var setting = await dbContext.MotorSettings
                                                .Where(x => x.ProductId == vendingItem.Id && x.Stock > 0 && x.IsActive == true && x.SoldOut == false)
                                                .OrderBy(x => x.CabinId)
                                                .ThenBy(x => x.MotorNumber)
                                                .FirstOrDefaultAsync();

                        if (setting is null)
                        {
                            logger.LogInformation($"Out of stock for product {vendingItem.Name}");
                            continue;
                        }


                        vendingItem.VendQtyStatus[i].Processing = true;
                        vendingItem.VendQtyStatus[i].ProcessValue = 10;
                        vendingItem.Status = "Starting.";

                        logger.LogInformation($"Start vending for product {vendingItem.Name} with quantity of {i + 1} on cabin : {setting.CabinId}, motor number : {setting.MotorNumber}");

                        bool write = modbus.RunMotor(setting.MotorNumber, setting.CabinId);

                        logger.LogInformation($"Motor write success : {write}");

                        if (!write)
                        {
                            logger.LogInformation($"Modbus write failed");
                            vendingItem.VendQtyStatus[i].Processing = false;
                            vendingItem.VendQtyStatus[i].ProcessValue = 0;
                            vendingItem.Status = "";
                            continue;
                        }

                        vendingItem.VendQtyStatus[i].ProcessValue = 20;

                        await Task.Delay(400);

                        int vendStatus = 0;
                        bool IsContinue = true;
                        bool IsRunning = false;
                        int wait_count = 0;
                        int anfiThieftTravelTime = 0;

                        while (IsContinue)
                        {
                            await Task.Delay(200);
                            wait_count++;
                            vendStatus = modbus.Status(setting.CabinId);
                            logger.LogInformation($"Process status : {vendStatus}");

                            switch (vendStatus)
                            {
                                case 0:
                                    vendingItem.Status = $"Starting{string.Concat(Enumerable.Repeat(".", wait_count % 4))}";
                                    break;
                                case 1:
                                    IsRunning = true;
                                    vendingItem.VendQtyStatus[i].ProcessValue += 5;
                                    vendingItem.Status = $"Processing{string.Concat(Enumerable.Repeat(".", wait_count % 4))}";
                                    break;
                                case 2:
                                    IsContinue = false;
                                    if (IsRunning)
                                    {
                                        vendingItem.Vend += 1;
                                        vendingItem.VendQtyStatus[i].StatusIcon = "Check";
                                        vendingItem.VendQtyStatus[i].Processing = false;
                                        vendingItem.VendQtyStatus[i].ProcessValue = 0;

                                        setting.Stock = setting.Stock - 1;
                                        setting.UpdatedOn = DateTime.Now;
                                        setting.IsViewed = false;
                                    }
                                    else
                                    {
                                        logger.LogInformation($"Got success command before motor rotate. Retry init.");
                                        vendingItem.VendQtyStatus[i].StatusIcon = "Close";
                                        vendingItem.VendQtyStatus[i].Processing = false;
                                        vendingItem.VendQtyStatus[i].ProcessValue = 0;

                                        i -= 1;
                                    }
                                    break;
                                case 3:
                                case 4:
                                case 5:
                                    IsContinue = false;
                                    logger.LogInformation($"Failed status code received. Retry init.");

                                    setting.SoldOut = true;
                                    setting.UpdatedOn = DateTime.Now;
                                    setting.IsViewed = false;

                                    vendingItem.VendQtyStatus[i].StatusIcon = "Close";
                                    vendingItem.VendQtyStatus[i].Processing = false;
                                    vendingItem.VendQtyStatus[i].ProcessValue = 0;
                                    i -= 1;
                                    break;
                                case 6:
                                case 7:
                                    break;
                                case 8:
                                    anfiThieftTravelTime++; 
                                    if(anfiThieftTravelTime > 25)
                                    {
                                        IsContinue = false;
                                        logger.LogInformation($"Anti-Thieft position reach failed");
                                    }
                                    break;


                                default:
                                    logger.LogInformation("Unknown status code received");
                                    break;
                            }

                            if (wait_count >= 60)
                            {
                                IsContinue = false;
                                logger.LogInformation($"Manual Time out");
                            }
                        }

                        vendingItem.Status = "";
                        await dbContext.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex.Message, ex);
                    }
                }
            }

            var order = await dbContext.Orders.FirstOrDefaultAsync(x => x.OrderNumber == DataStore.orderNumber);

            if (order is not null)
            {
                foreach (var item in order.Items!)
                {
                    var vendItem = vendingItems.FirstOrDefault(x => x.Id == item.ProductId);
                    if (vendItem is not null)
                    {
                        item.VendQty = vendItem.Vend;
                        item.UpdatedOn = DateTime.Now;
                        item.IsViewed = false;
                    }
                }
                order.IsViewed = false;
                await dbContext.SaveChangesAsync();
            }

            if (DataStore.AntiThieft)
                modbus.CloseAntiThieftDoor();


        }
        catch (Exception exOut)
        {
            logger.LogError(exOut, exOut.Message);
        }
    }

    public async Task CheckRefund()
    {
        try
        {

            var order = await dbContext.Orders.FirstOrDefaultAsync(x => x.OrderNumber == DataStore.orderNumber);

            if (order is null)
                return;

            double VendTotal = vendingItems.Sum(x => x.VendAmount);

            logger.LogInformation($"Total Vended Amount : {VendTotal}");

            double balance = order.PaidAmount - VendTotal;
            string txtMsg = string.Empty;

            if (balance > 0)
            {
                string msg = string.Join(" | ", vendingItems.Where(x => x.Qty != x.Vend)
                                                            .Select(x => $"{x.Name}-{x.Price}x{x.Qty - x.Vend}={(x.Qty - x.Vend) * x.Price}")
                                                            .ToList());
                logger.LogInformation($"Refund : {balance}, Msg :{msg}");

                switch (order.PaymentType)
                {
                    case "UPI":
                        {
                            txtMsg = $"Sorry for the Inconvenience, Your balance of Rs.{balance} has been refunded. It will reflect your account within 48hrs.";
                            bool isRefund = await RefundUPI(order.OrderNumber, balance, msg);
                            if (isRefund)
                            {
                                order.IsRefunded = true;
                                order.RefundedAmount = balance;
                                order.IsViewed = false;
                                logger.LogInformation($"UPI refund success");
                            }
                        }
                        break;
                    case "ACCOUNT":
                        {
                            bool isRefund = true;
                            if (paymentConfig.AccountConfig.Plan == StrDir.AccountPlan.PREPAID)
                                isRefund = await RefundPrePaidAccount(order.UserId, order.Id, balance);

                            if (isRefund)
                            {
                                order.IsRefunded = true;
                                order.RefundedAmount = balance;
                                order.IsViewed = false;
                                logger.LogInformation($"Account refund for {order.UserId} of {balance} success");
                            }
                        }
                        break;
                    case "CASH":
                        {
                            await RefundCash((int)balance, msg);
                            txtMsg = $"Sorry for the Inconvenience, to get your balance of Rs.{balance} please call the support team";
                            order.IsRefunded = true;
                            order.RefundedAmount = balance;
                            order.IsViewed = false;
                        }
                        break;
                    default:
                        break;
                }
            }

            await dbContext.SaveChangesAsync();

            if (!string.IsNullOrEmpty(txtMsg))
            {
                lblMessage.Text = txtMsg;
                await Task.Delay(5000);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    public async Task UpdateOrder()
    {
        try
        {
            var order = await dbContext.Orders.FirstOrDefaultAsync(x => x.OrderNumber == DataStore.orderNumber);

            if (order is null)
                return;

            double VendTotal = vendingItems.Sum(x => x.VendAmount);
            order.Total = VendTotal;
            order.Status = StrDir.OrderStatus.SUCCESS;
            order.IsViewed = false;
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    private async Task<bool> RefundUPI(string orderNumber, double RefundAmount, string msg = "")
    {
        try
        {
            QrRefundDto refundDto = new QrRefundDto(orderNumber, RefundAmount, msg);
            var response = await httpClient.PostAsync($"api/payments/upi/refund", refundDto);
            string respText = await response.Content.ReadAsStringAsync();
            logger.LogInformation(respText);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            return false;
        }
    }

    private async Task<bool> RefundPrePaidAccount(string UserId, string OrderId, double RefundAmount)
    {
        try
        {
            AppUserRefundDto refundDto = new AppUserRefundDto(UserId, OrderId, RefundAmount);
            var response = await httpClient.PostAsync($"api/appuser/refund", refundDto);
            string respText = await response.Content.ReadAsStringAsync();
            logger.LogInformation(respText);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            return false;
        }
    }

    private async Task RefundCash(int RefundAmount, string msg = "")
    {
        try
        {
            string mobileNumber = await GetMobileNumber();

            if (mobileNumber.Length != 10)
                mobileNumber = "0000000000";

            logger.LogInformation($"Cash Refund. Order : {DataStore.orderNumber}, Mobile : {mobileNumber}, Amount : {RefundAmount}");

            CashRefund cashRefund = new CashRefund()
            {
                RefId = Guid.NewGuid().ToString("N").ToUpper(),
                OrderNumber = DataStore.orderNumber,
                MobileNumber = mobileNumber,
                Denomination = msg,
                Amount = RefundAmount,
                CancelOn = DateTime.Now,
                IsViewed = false
            };

            dbContext.CashRefunds.Add(cashRefund);

            var order = await dbContext.Orders.FirstAsync(x => x.OrderNumber == DataStore.orderNumber);

            order.IsPaid = true;
            order.IsRefunded = true;
            order.RefundedAmount = RefundAmount;
            order.IsViewed = false;
            await dbContext.SaveChangesAsync();

            bool success = await PostCashRefundToServer(cashRefund);
            if (success)
            {
                cashRefund.IsViewed = true;
                await dbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    private async Task<string> GetMobileNumber()
    {
        try
        {
            if (DialogHost.IsDialogOpen("pgVendingPageHost"))
                DialogHost.Close("pgVendingPageHost");

            var sampleMessageDialog = new MobileNumberDialog { Message = { Text = "To get refund \n Please enter your mobile number" } };
            var result = await DialogHost.Show(sampleMessageDialog, "pgVendingPageHost");
            var isEntered = Convert.ToBoolean(result);
            if (isEntered)
                return sampleMessageDialog.txtInput.Text.Trim();
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
        return string.Empty;
    }

    private async Task<bool> PostCashRefundToServer(CashRefund cashRefund)
    {
        try
        {
            CashRefundDto cashRefundDto = new CashRefundDto()
            {
                OrderNumber = cashRefund.OrderNumber,
                MobileNumber = cashRefund.MobileNumber,
                Amount = cashRefund.Amount,
                Denomination = cashRefund.Denomination,
                CancelOn = cashRefund.CancelOn
            };

            var response = await httpClient.PostAsync("api/cashrefund", cashRefundDto);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                string respText = await response.Content.ReadAsStringAsync();
                logger.LogInformation($"Cash refund send failed. Status Code : {response.StatusCode}, Response : {respText}");
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            return false;
        }
    }


}
