using CMS.Dto;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using System.Data;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using VM.Domains;
using VM.Dtos;
using VM.Services.Interfaces;

namespace VM.Services;

public class ReportService : IReportService
{
    private readonly AppDbContext dbContext;
    private readonly PaymentConfig paymentConfig;
    private readonly IServerClient httpClient;
    private readonly ILogger<ReportService> logger;


    public ReportService(AppDbContext dbContext, PaymentConfig paymentConfig, IServerClient httpClient, ILogger<ReportService> logger)
    {
        this.dbContext = dbContext;
        this.paymentConfig = paymentConfig;
        this.httpClient = httpClient;
        this.logger = logger;
    }

    public async Task<bool> SendTodayAutomatedReport()
    {
        try
        {
            DateTime previousDay = DateTime.Now.AddDays(-1);
            DateTime from = new DateTime(previousDay.Year, previousDay.Month, previousDay.Day, 0, 0, 0);
            DateTime to = new DateTime(previousDay.Year, previousDay.Month, previousDay.Day, 23, 59, 59);
            string fileName = $"AutomatedDailyReport-{DataStore.MachineInfo.VendorShortName}{DataStore.AppType}{DataStore.MachineInfo.MachineNumber}-{previousDay.ToString("ddMMMyyyy")}.xls";
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports", fileName);

            if (!File.Exists(path))
            {
                var result = await EmailReport(from, to, path, "AutomatedDailyReport");
                if (!result.Success)
                    File.Delete(path);

                return result.Success;
            }
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            return false;
        }
    }

    public async Task<ReportStatusDto> EmailReport(DateTime from, DateTime to, string path, string reportType)
    {
        try
        {
            var success = await GenerateExcelReport(from, to, path);
            if (!success)
                return new ReportStatusDto(false, "Report generation failed");

            using var form = new MultipartFormDataContent();
            using var fileContent = new ByteArrayContent(await File.ReadAllBytesAsync(path));
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
            form.Add(fileContent, "File", Path.GetFileName(path));
            form.Add(new StringContent(from.ToString("yyyy-MM-dd HH:mm:ss")), "StartDate");
            form.Add(new StringContent(to.ToString("yyyy-MM-dd HH:mm:ss")), "EndDate");
            form.Add(new StringContent(reportType), "ReportType");

            var response = await httpClient.PostAsync("api/reports/email", form);
            var respText = await response.Content.ReadAsStringAsync();
            logger.LogInformation($"Email Send. Status Code : {response.StatusCode}, Response : {respText}");

            if (!response.IsSuccessStatusCode)
                return new ReportStatusDto(false, "Mail sent failed");

            var respDto = JsonConvert.DeserializeObject<SuccessDto>(respText);
            return new ReportStatusDto(true, respDto?.Message ?? "Mail Send Success");
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            return new ReportStatusDto(false, "Error on sending the report");
        }
    }

    public async Task SendReportToServer(string requestId, DateTime from, DateTime to)
    {
        try
        {
            string fileName = $"{DataStore.MachineInfo.VendorShortName}-{DataStore.AppType[0]}-{DataStore.MachineInfo.MachineNumber}-{requestId}.xls";
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports", fileName);
            var success = await GenerateExcelReport(from, to, path);

            if (!success)
            {
                logger.LogInformation("Report generation failed for live report request");
                return;
            }

            using var form = new MultipartFormDataContent();
            using var fileContent = new ByteArrayContent(await File.ReadAllBytesAsync(path));
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
            form.Add(fileContent, "File", Path.GetFileName(path));
            form.Add(new StringContent(requestId), "RequestId");

            var response = await httpClient.PostAsync("api/reports/live/machine/response", form);
            var respText = await response.Content.ReadAsStringAsync();
            logger.LogInformation($"Report Send. Status Code : {response.StatusCode}, Response : {respText}");

        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    public async Task<bool> GenerateExcelReport(DateTime from, DateTime to, string path)
    {
        try
        {
            HSSFWorkbook wb = new HSSFWorkbook();

            #region Style
            var font = wb.CreateFont();
            font.FontHeightInPoints = 12;
            font.FontName = "Calibri";
            font.IsBold = true;


            var style = wb.CreateCellStyle();
            style.WrapText = true;
            style.Alignment = HorizontalAlignment.Center;
            style.VerticalAlignment = VerticalAlignment.Center;
            style.SetFont(font);


            var sum_style = wb.CreateCellStyle();
            sum_style.WrapText = true;
            sum_style.Alignment = HorizontalAlignment.Right;
            sum_style.VerticalAlignment = VerticalAlignment.Center;
            sum_style.SetFont(font);
            #endregion

            #region Sales Report

            ISheet sheet1 = wb.CreateSheet("Sales");
            var sales = await GetSales(from, to);

            int r = 0;
            IRow row = sheet1.CreateRow(r++);
            row.Height = (short)(row.Height * 3);
            ICell cell = row.CreateCell(0);
            cell.SetCellValue(("Sales Report between " + from.ToString("dd-MM-yyyy") + " and " + to.ToString("dd-MM-yyyy")).ToUpper());


            row = sheet1.CreateRow(r++);
            row.Height = (short)(row.Height * 1.5);

            List<int> maximumLengthForColumns = [3, 12, 15, 10, 10, 10, 10, 100];

            row.CreateCell(0).SetCellValue("Sno");
            row.CreateCell(1).SetCellValue("Order Number");
            row.CreateCell(2).SetCellValue("Date");
            row.CreateCell(3).SetCellValue("Type");
            row.CreateCell(4).SetCellValue("Amount");
            row.CreateCell(5).SetCellValue("Paid");
            row.CreateCell(6).SetCellValue("Refund");
            row.CreateCell(7).SetCellValue("Items");

            if (paymentConfig.Account)
            {
                row.CreateCell(8).SetCellValue("UserId");
                maximumLengthForColumns = [3, 12, 15, 10, 10, 10, 10, 100, 12];
            }

            foreach (var sale in sales)
            {
                row = sheet1.CreateRow(r++);
                row.CreateCell(0).SetCellValue(r - 2);
                row.CreateCell(1).SetCellValue(sale.OrderNumber);
                row.CreateCell(2).SetCellValue(sale.OrderDate.ToString("dd-MM-yyyy hh:mm tt"));
                row.CreateCell(3).SetCellValue(sale.PaymentType);
                row.CreateCell(4).SetCellValue(sale.Total);
                row.CreateCell(5).SetCellValue(sale.PaidAmount);
                row.CreateCell(6).SetCellValue(sale.RefundedAmount);
                row.CreateCell(7).SetCellValue(string.Join(Environment.NewLine, sale.Items?.Select(x => $"{x.ProductName} - {x.Price} X {x.VendQty}").ToList() ?? []));
                if (paymentConfig.Account)
                {
                    row.CreateCell(8).SetCellValue(sale.UserId);
                }
            }

            sheet1.GetRow(0).GetCell(0).CellStyle = style;
            sheet1.AddMergedRegion(new CellRangeAddress(0, 0, 0, maximumLengthForColumns.Count - 1));
            sheet1.SetAutoFilter(new CellRangeAddress(1, 1, 0, maximumLengthForColumns.Count - 1));
            sheet1.CreateFreezePane(0, 2);
            for (int i = 0; i < maximumLengthForColumns.Count; i++)
            {
                sheet1.SetColumnWidth(i, ((maximumLengthForColumns[i] > 100 ? 100 : maximumLengthForColumns[i]) + 8) * 255);
                sheet1.GetRow(1).GetCell(i).CellStyle = style;
            }

            if (r > 4)
            {
                row = sheet1.CreateRow(r++);
                row.Height = (short)(row.Height * 1.5);

                cell = row.CreateCell(4);
                cell.SetCellValue(sales.Sum(x => x.Total));
                cell.CellStyle = sum_style;
            }

            #endregion

            #region User Sales Report

            if (paymentConfig.Account)
            {
                ISheet sheetUsers = wb.CreateSheet("Sales-Users");

                r = 0;
                row = sheetUsers.CreateRow(r++);
                row.Height = (short)(row.Height * 3);
                cell = row.CreateCell(0);
                cell.SetCellValue(("Sales Report - User Based between " + from.ToString("dd-MM-yyyy") + " and " + to.ToString("dd-MM-yyyy")).ToUpper());

                row = sheetUsers.CreateRow(r++);
                row.Height = (short)(row.Height * 1.5);
                row.CreateCell(0).SetCellValue("Sno");
                row.CreateCell(1).SetCellValue("UserId");
                row.CreateCell(2).SetCellValue("Name");
                row.CreateCell(3).SetCellValue("Amount");
                row.CreateCell(4).SetCellValue("Items");

                maximumLengthForColumns = [3, 15, 20, 10, 50];

                var salesUsers = await GetSalesByUser(from, to);

                foreach (var sale in salesUsers)
                {
                    row = sheetUsers.CreateRow(r++);
                    row.CreateCell(0).SetCellValue(r - 2);
                    row.CreateCell(1).SetCellValue(sale.UserId);
                    row.CreateCell(2).SetCellValue(sale.UserName);
                    row.CreateCell(3).SetCellValue(sale.Amount);
                    row.CreateCell(4).SetCellValue(sale.Items);
                }

                sheetUsers.GetRow(0).GetCell(0).CellStyle = style;

                sheetUsers.AddMergedRegion(new CellRangeAddress(0, 0, 0, 4));
                sheetUsers.SetAutoFilter(new CellRangeAddress(1, 1, 0, 4));
                sheetUsers.CreateFreezePane(0, 2);

                for (int i = 0; i < maximumLengthForColumns.Count; i++)
                {
                    sheetUsers.SetColumnWidth(i, ((maximumLengthForColumns[i] > 100 ? 100 : maximumLengthForColumns[i]) + 8) * 255);
                    sheetUsers.GetRow(1).GetCell(i).CellStyle = style;
                }

                if (r > 4)
                {
                    row = sheetUsers.CreateRow(r++);
                    row.Height = (short)(row.Height * 1.5);

                    cell = row.CreateCell(3);
                    cell.SetCellValue(salesUsers.Sum(x => x.Amount));
                    cell.CellStyle = sum_style;
                }
            }

            #endregion

            #region Produt Sales Report

            ISheet sheet2 = wb.CreateSheet("Sales-Product");

            r = 0;
            row = sheet2.CreateRow(r++);
            row.Height = (short)(row.Height * 3);
            cell = row.CreateCell(0);
            cell.SetCellValue(("Sales Report - Poduct Based between " + from.ToString("dd-MM-yyyy") + " and " + to.ToString("dd-MM-yyyy")).ToUpper());

            row = sheet2.CreateRow(r++);
            row.Height = (short)(row.Height * 1.5);
            row.CreateCell(0).SetCellValue("Sno");
            row.CreateCell(1).SetCellValue("Category");
            row.CreateCell(2).SetCellValue("Product Name");
            row.CreateCell(3).SetCellValue("Price");
            row.CreateCell(4).SetCellValue("Quantity");
            row.CreateCell(5).SetCellValue("Amount");

            maximumLengthForColumns = [3, 15, 20, 10, 10, 10];

            var salesProducts = await GetSalesByProduct(from, to);

            foreach (var sale in salesProducts)
            {
                row = sheet2.CreateRow(r++);
                row.CreateCell(0).SetCellValue(r - 2);
                row.CreateCell(1).SetCellValue(sale.Category);
                row.CreateCell(2).SetCellValue(sale.ProductName);
                row.CreateCell(3).SetCellValue(sale.Price);
                row.CreateCell(4).SetCellValue(sale.Quantity);
                row.CreateCell(5).SetCellValue(sale.Amount);
            }

            sheet2.GetRow(0).GetCell(0).CellStyle = style;

            sheet2.AddMergedRegion(new CellRangeAddress(0, 0, 0, 5));
            sheet2.SetAutoFilter(new CellRangeAddress(1, 1, 0, 5));
            sheet2.CreateFreezePane(0, 2);

            for (int i = 0; i < maximumLengthForColumns.Count; i++)
            {
                sheet2.SetColumnWidth(i, ((maximumLengthForColumns[i] > 100 ? 100 : maximumLengthForColumns[i]) + 8) * 255);
                sheet2.GetRow(1).GetCell(i).CellStyle = style;
            }

            if (r > 4)
            {
                row = sheet2.CreateRow(r++);
                row.Height = (short)(row.Height * 1.5);

                cell = row.CreateCell(5);
                cell.SetCellValue(salesProducts.Sum(x => x.Amount));
                cell.CellStyle = sum_style;
            }
            #endregion

            if (DataStore.AppType == StrDir.AppType.VM)
            {
                #region Stock Report

                ISheet sheet3 = wb.CreateSheet("Stock");

                r = 0;
                row = sheet3.CreateRow(r++);
                row.Height = (short)(row.Height * 3);
                cell = row.CreateCell(0);
                cell.SetCellValue("Stock");

                row = sheet3.CreateRow(r++);
                row.Height = (short)(row.Height * 1.5);
                row.CreateCell(0).SetCellValue("Sno");
                row.CreateCell(1).SetCellValue("Product Name");
                row.CreateCell(2).SetCellValue("Price");
                row.CreateCell(3).SetCellValue("Capacity");
                row.CreateCell(4).SetCellValue("Stock");
                row.CreateCell(5).SetCellValue("Amount");

                maximumLengthForColumns = [3, 20, 10, 10, 10, 10];

                var stocks = await GetCurrentStock();

                foreach (var stock in stocks)
                {
                    row = sheet3.CreateRow(r++);
                    row.CreateCell(0).SetCellValue(r - 2);
                    row.CreateCell(1).SetCellValue(stock.ProductName);
                    row.CreateCell(2).SetCellValue(stock.Price);
                    row.CreateCell(3).SetCellValue(stock.Capacity);
                    row.CreateCell(4).SetCellValue(stock.Stock);
                    row.CreateCell(5).SetCellValue(stock.Amount);
                }

                sheet3.GetRow(0).GetCell(0).CellStyle = style;

                sheet3.AddMergedRegion(new CellRangeAddress(0, 0, 0, 5));
                sheet3.SetAutoFilter(new CellRangeAddress(1, 1, 0, 5));
                sheet3.CreateFreezePane(0, 2);

                for (int i = 0; i < maximumLengthForColumns.Count; i++)
                {
                    sheet3.SetColumnWidth(i, ((maximumLengthForColumns[i] > 100 ? 100 : maximumLengthForColumns[i]) + 8) * 255);
                    sheet3.GetRow(1).GetCell(i).CellStyle = style;
                }

                if (r > 4)
                {
                    row = sheet3.CreateRow(r++);
                    row.Height = (short)(row.Height * 1.5);

                    cell = row.CreateCell(5);
                    cell.SetCellValue(stocks.Sum(x => x.Amount));
                    cell.CellStyle = sum_style;
                }

                #endregion

                #region Refill 

                ISheet sheet4 = wb.CreateSheet("Refill");

                r = 0;
                row = sheet4.CreateRow(r++);
                row.Height = (short)(row.Height * 3);
                cell = row.CreateCell(0);
                cell.SetCellValue(("Refill Report between " + from.ToString("dd-MM-yyyy") + " and " + to.ToString("dd-MM-yyyy")).ToUpper());

                row = sheet4.CreateRow(r++);
                row.Height = (short)(row.Height * 1.5);
                row.CreateCell(0).SetCellValue("Sno");
                row.CreateCell(1).SetCellValue("Date");
                row.CreateCell(2).SetCellValue("Sprial");
                row.CreateCell(3).SetCellValue("Product Name");
                row.CreateCell(4).SetCellValue("Price");
                row.CreateCell(5).SetCellValue("Refill");
                row.CreateCell(6).SetCellValue("Amount");

                maximumLengthForColumns = [3, 15, 10, 20, 10, 10, 10];

                var refills = await GetRefillItems(from, to);

                foreach (var refill in refills)
                {
                    row = sheet4.CreateRow(r++);
                    row.CreateCell(0).SetCellValue(r - 2);
                    row.CreateCell(1).SetCellValue(refill.RefilledOn.ToString("dd-MM-yyyy hh:mm tt"));
                    row.CreateCell(2).SetCellValue(refill.MotorNumber);
                    row.CreateCell(3).SetCellValue(refill.ProductName);
                    row.CreateCell(4).SetCellValue(refill.Price);
                    row.CreateCell(5).SetCellValue(refill.RefillCount);
                    row.CreateCell(6).SetCellValue(refill.Amount);
                }

                sheet4.GetRow(0).GetCell(0).CellStyle = style;

                sheet4.AddMergedRegion(new CellRangeAddress(0, 0, 0, 6));
                sheet4.SetAutoFilter(new CellRangeAddress(1, 1, 0, 6));
                sheet4.CreateFreezePane(0, 2);

                for (int i = 0; i < maximumLengthForColumns.Count; i++)
                {
                    sheet4.SetColumnWidth(i, ((maximumLengthForColumns[i] > 100 ? 100 : maximumLengthForColumns[i]) + 8) * 255);
                    sheet4.GetRow(1).GetCell(i).CellStyle = style;
                }

                if (r > 4)
                {
                    row = sheet4.CreateRow(r++);
                    row.Height = (short)(row.Height * 1.5);

                    cell = row.CreateCell(6);
                    cell.SetCellValue(refills.Sum(x => x.Amount));
                    cell.CellStyle = sum_style;
                }

                #endregion

                #region Stock Cleared 

                ISheet sheet5 = wb.CreateSheet("Stock-Return");

                r = 0;
                row = sheet5.CreateRow(r++);
                row.Height = (short)(row.Height * 3);
                cell = row.CreateCell(0);
                cell.SetCellValue(("Stock - Return Report between " + from.ToString("dd-MM-yyyy") + " and " + to.ToString("dd-MM-yyyy")).ToUpper());

                row = sheet5.CreateRow(r++);
                row.Height = (short)(row.Height * 1.5);
                row.CreateCell(0).SetCellValue("Sno");
                row.CreateCell(1).SetCellValue("Date");
                row.CreateCell(2).SetCellValue("Sprial");
                row.CreateCell(3).SetCellValue("Product Name");
                row.CreateCell(4).SetCellValue("Price");
                row.CreateCell(5).SetCellValue("Quantity");
                row.CreateCell(6).SetCellValue("Amount");

                maximumLengthForColumns = [3, 15, 10, 20, 10, 10, 10];

                var stockCleared = await GetStockCleared(from, to);

                foreach (var cleared in stockCleared)
                {
                    row = sheet5.CreateRow(r++);
                    row.CreateCell(0).SetCellValue(r - 2);
                    row.CreateCell(1).SetCellValue(cleared.ClearedOn.ToString("dd-MM-yyyy hh:mm tt"));
                    row.CreateCell(2).SetCellValue(cleared.MotorNumber);
                    row.CreateCell(3).SetCellValue(cleared.ProductName);
                    row.CreateCell(4).SetCellValue(cleared.Price);
                    row.CreateCell(5).SetCellValue(cleared.RefillCount);
                    row.CreateCell(6).SetCellValue(cleared.Amount);
                }

                sheet5.GetRow(0).GetCell(0).CellStyle = style;

                sheet5.AddMergedRegion(new CellRangeAddress(0, 0, 0, 6));
                sheet5.SetAutoFilter(new CellRangeAddress(1, 1, 0, 6));
                sheet5.CreateFreezePane(0, 2);

                for (int i = 0; i < maximumLengthForColumns.Count; i++)
                {
                    sheet5.SetColumnWidth(i, ((maximumLengthForColumns[i] > 100 ? 100 : maximumLengthForColumns[i]) + 8) * 255);
                    sheet5.GetRow(1).GetCell(i).CellStyle = style;
                }

                if (r > 4)
                {
                    row = sheet5.CreateRow(r++);
                    row.Height = (short)(row.Height * 1.5);

                    cell = row.CreateCell(6);
                    cell.SetCellValue(stockCleared.Sum(x => x.Amount));
                    cell.CellStyle = sum_style;
                }

                #endregion
            }

            FileStream file = new FileStream(path, FileMode.Create);
            wb.Write(file);
            file.Close();

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            return false;
        }
    }

    public async Task<List<Order>> GetSales(DateTime from, DateTime to)
    {
        try
        {
            var sales = await dbContext.Orders
                                        .Include(x => x.Items)
                                        .Where(x => x.Status == StrDir.OrderStatus.SUCCESS && x.OrderDate >= from && x.OrderDate <= to)
                                        .OrderBy(x => x.OrderDate)
                                        .ToListAsync();
            return sales ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            return [];
        }
    }

    public async Task<List<SalesByUserDto>> GetSalesByUser(DateTime from, DateTime to)
    {
        try
        {

            var rawsales = await dbContext.Orders
                                        .Include(x => x.Items)
                                        .Where(x => x.Status == StrDir.OrderStatus.SUCCESS && x.PaymentType == StrDir.PaymentType.ACCOUNT && x.OrderDate >= from && x.OrderDate <= to)
                                        .AsNoTracking()
                                        .ToListAsync();

            var appusers = await dbContext.AppUsers.AsNoTracking().ToListAsync();

            var sales = rawsales.GroupBy(x => x.UserId)
                                .Select(g =>
                                        new SalesByUserDto(
                                            g.Key,
                                            appusers.FirstOrDefault(x => x.Id == g.Key)?.Name ?? "",
                                            g.Sum(o => o.Total),
                                            string.Join("\n",
                                            g.SelectMany(x => x.Items!)
                                            .GroupBy(i => i.ProductId)
                                            .Select(i => $"{i.First().ProductName} - {i.First().Price} x {i.Sum(x => x.VendQty)}").ToList())
                                )).ToList();

            return sales ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            return [];
        }
    }

    public async Task<List<SalesByProductDto>> GetSalesByProduct(DateTime from, DateTime to)
    {
        try
        {
            var sales = await GetSales(from, to);
            var salesItems = sales.SelectMany(x => x.Items ?? []).ToList();
            var productsIds = salesItems.Select(x => x.ProductId).Distinct().ToList();
            var products = await dbContext.Products.Include(x => x.Category).Where(x => productsIds.Contains(x.Id)).ToListAsync();



            var salesByProducts = (from si in salesItems
                                   group si by si.ProductId into gi
                                   select new
                                   SalesByProductDto(
                                       products.FirstOrDefault(x => x.Id == gi.Key)?.Category?.Name ?? "",
                                       gi.Key,
                                       gi.First().ProductName,
                                       gi.First().Price,
                                       gi.Sum(x => x.VendQty),
                                       gi.Sum(x => x.VendQty * x.Price))).ToList();


            return salesByProducts.OrderBy(x => x.Category).ThenBy(x => x.ProductName).ToList() ?? [];

        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            return [];
        }
    }

    public async Task<List<CurrentStockByProductDto>> GetCurrentStock()
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
            var stocks = (from ts in trayStocks
                          join p in Products on ts.ProductId equals p.Id
                          select new CurrentStockByProductDto
                          (
                              p.Name,
                              p.Price,
                              ts.Capacity,
                              ts.Stock,
                              p.Price * ts.Stock
                          )).ToList();
            return stocks.OrderBy(x => x.ProductName).ToList() ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            return [];
        }
    }

    public async Task<List<RefillReportDto>> GetRefillItems(DateTime from, DateTime to)
    {
        try
        {
            var refills = await dbContext.Refills.Where(x => x.RefilledOn >= from && x.RefilledOn <= to).ToListAsync();
            var productIds = refills.Select(x => x.ProductId).ToList();
            var Products = await dbContext.Products.Where(x => productIds.Contains(x.Id)).ToListAsync();

            var result = (from r in refills
                          join p in Products on r.ProductId equals p.Id
                          select new RefillReportDto(r.RefilledOn, r.MotorNumber, r.ProductName, p.Price, r.Quantity, p.Price * r.Quantity)).ToList();

            return result.OrderBy(x => x.RefilledOn).ToList() ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            return [];
        }
    }

    public async Task<List<StockClearedReportDto>> GetStockCleared(DateTime from, DateTime to)
    {
        try
        {
            var cleared = await dbContext.StockCleared.Where(x => x.ClearedOn >= from && x.ClearedOn <= to).ToListAsync();
            var productIds = cleared.Select(x => x.ProductId).ToList();
            var Products = await dbContext.Products.Where(x => productIds.Contains(x.Id)).ToListAsync();

            var result = (from r in cleared
                          join p in Products on r.ProductId equals p.Id
                          select new StockClearedReportDto(r.ClearedOn, r.MotorNumber, r.ProductName, p.Price, r.Quantity, p.Price * r.Quantity)).ToList();

            return result.OrderBy(x => x.ClearedOn).ToList() ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            return [];
        }
    }
}
