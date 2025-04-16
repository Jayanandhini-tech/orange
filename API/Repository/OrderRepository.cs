using CMS.API.Domains;
using CMS.API.Repository.IRepository;
using CMS.API.Services.Interfaces;
using CMS.Dto;
using CMS.Dto.Reports;
using Microsoft.EntityFrameworkCore;

namespace CMS.API.Repository;

public class OrderRepository : Repository<Order>, IOrderRepository
{
    private readonly AppDBContext db;
    private readonly ITenant tenant;

    public OrderRepository(AppDBContext db, ITenant tenant) : base(db, tenant)
    {
        this.db = db;
        this.tenant = tenant;
    }

    public async Task<int> GetNextBillNumber()
    {
        int id = await db.Orders.Where(x => x.VendorId == tenant.VendorId && !string.IsNullOrEmpty(x.PaymentId)).CountAsync();
        return ++id;
    }

    public async Task<Order?> GetOrderWithAllIncludes(string orderId)
    {
        var order = await db.Orders
                              .Include(x => x.Items)
                              .Include(x => x.Payment)
                              .Where(x => x.Id == orderId)
                              .FirstOrDefaultAsync();
        return order;
    }

    public async Task<Order?> GetOrderUsingOrderNumber(string orderNumber, bool tracked = false)
    {
        var orderQuery = db.Orders
                            .AsSplitQuery()
                            .Include(x => x.Items)                           
                            .Where(x => x.OrderNumber == orderNumber);

        if (!tracked)
            orderQuery.AsNoTracking();

        var order = await orderQuery.FirstOrDefaultAsync();
        return order;
    }

    public async Task<List<Order>> GetUserRecentOrders(string userId)
    {
        var orders = await db.Orders
                        .AsNoTracking().AsSplitQuery()
                        .Include(x => x.Items)
                        .Where(o => o.VendorId == tenant.VendorId &&
                                    o.PaymentType == StrDir.PaymentType.ACCOUNT &&
                                    o.Status == StrDir.OrderStatus.SUCCESS &&
                                    o.AppUserId == userId)
                        .OrderByDescending(x => x.OrderDate)
                        .Take(10)
                        .ToListAsync();
        return orders ?? [];
    }

    public async Task<List<OrderReportDto>> GetSalesReportAsync(string machineId, DateTime fromDate, DateTime toDate)
    {
        var query = db.Orders
                        .Include(x => x.Items)
                        .Where(x => x.VendorId == tenant.VendorId && x.Status == StrDir.OrderStatus.SUCCESS && x.OrderDate >= fromDate && x.OrderDate <= toDate);

        if (!string.IsNullOrEmpty(machineId))
            query = query.Where(x => x.MachineId == machineId);

        var orders = await query.AsSplitQuery()
                                .OrderBy(x => x.OrderDate)
                                .Select(o => new OrderReportDto()
                                {
                                    OrderDate = o.OrderDate,
                                    OrderNumber = o.OrderNumber,
                                    PaymentType = o.PaymentType,
                                    Amount = o.Total,
                                    Paid = o.PaidAmount,
                                    Refund = o.RefundedAmount,
                                    UserId = o.AppUserId,
                                    OrderItems = o.Items!.Select(p => new OrderItemReportDto()
                                    {
                                        ProductName = p.ProductName,
                                        Price = p.Price,
                                        Quantity = p.VendQty,
                                        Amount = p.Price * p.VendQty
                                    }).ToList(),
                                }).ToListAsync();
        return orders ?? [];
    }

    public async Task<List<OrderItemReportDto>> GetProductBasedSales(string machineId, DateTime fromDate, DateTime toDate)
    {
        var query = db.Orders
                       .Include(x => x.Items)
                       .Where(x => x.VendorId == tenant.VendorId && x.Status == StrDir.OrderStatus.SUCCESS && x.OrderDate >= fromDate && x.OrderDate <= toDate);

        if (!string.IsNullOrEmpty(machineId))
            query = query.Where(x => x.MachineId == machineId);

        var orders = await query.SelectMany(x => x.Items!)
                                .GroupBy(x => new { x.ProductName, x.Price })
                                .Select(g => new OrderItemReportDto()
                                {
                                    ProductName = g.Key.ProductName,
                                    Price = g.Key.Price,
                                    Quantity = g.Sum(x => x.VendQty),
                                    Amount = g.Sum(x => x.Price * x.VendQty)
                                })
                                .AsSplitQuery()
                                .ToListAsync();

        return orders.OrderBy(x => x.ProductName).ToList() ?? [];
    }


    public async Task<List<SalesUsersDto>> GetUserBasedSales(string machineId, DateTime fromDate, DateTime toDate)
    {
        var query = db.Orders
                       .Include(x => x.Items)
                       .Where(x => x.VendorId == tenant.VendorId
                                && x.Status == StrDir.OrderStatus.SUCCESS
                                && x.PaymentType == StrDir.PaymentType.ACCOUNT
                                && x.OrderDate >= fromDate
                                && x.OrderDate <= toDate);

        if (!string.IsNullOrEmpty(machineId))
            query = query.Where(x => x.MachineId == machineId);

        var rawOrders = await query
                                .GroupBy(x => x.AppUserId)
                                .Select(g => new
                                {
                                    UserId = g.Key,
                                    Amount = g.Sum(x => x.Total),
                                    Items = g.SelectMany(x => x.Items!).ToList()
                                })
                                .AsSplitQuery()
                                .ToListAsync();

        var orders = rawOrders.Select(x => new
        {
            x.UserId,
            x.Amount,
            items = x.Items.GroupBy(x => x.ProductId)
                .Select(g => new
                {
                    g.First().ProductName,
                    g.First().Price,
                    Qty = g.Sum(x => x.VendQty)
                })
                .Select(i => $"{i.ProductName}-{i.Price}x{i.Qty}").ToList()
        }).ToList();

        var usersId = orders.Select(x => x.UserId).Distinct().ToList();
        var users = await db.AppUsers.Where(x => usersId.Contains(x.Id)).Select(x => new { x.Id, x.Name }).ToListAsync();

        var sales = (from o in orders
                     join u in users on o.UserId equals u.Id into gj
                     from j in gj.DefaultIfEmpty()
                     select new SalesUsersDto(o.UserId, j?.Name?.Trim().ToLower() == "name" ? "" : j?.Name ?? "", o.Amount, o.items)).ToList();

        return sales.OrderBy(x => x.UserId).ToList() ?? [];
    }


    public async Task<OrderReportResponseDto> GetOrderReportAsync(DateTime fromDate, DateTime toDate, int limit, int skip)
    {
        var orders = await db.Orders
                .Where(x => x.VendorId == tenant.VendorId && x.OrderDate > fromDate.Date && x.OrderDate < toDate.Date.AddDays(1))
                 .Include(x => x.Items)
                  .OrderBy(x => x.OrderDate)
                  .Skip(skip).Take(limit)
                  .Select(o => new OrderReportDto()
                  {
                      OrderDate = o.OrderDate,
                      OrderNumber = o.OrderNumber,
                      PaymentType = o.PaymentType,
                      Amount = o.Total,
                      Paid = o.PaidAmount,
                      Refund = o.RefundedAmount,
                      UserId = o.AppUserId,
                      OrderItems = o.Items!.Select(p => new OrderItemReportDto()
                      {
                          ProductName = p.ProductName,
                          Price = p.Price,
                          Quantity = p.Qty,
                          Amount = p.Price * p.Qty
                      }).ToList(),
                  }).ToListAsync();

        int total = await db.Orders.Where(x => x.VendorId == tenant.VendorId && x.OrderDate > fromDate.Date && x.OrderDate < toDate.Date.AddDays(1)).CountAsync();

        return new OrderReportResponseDto() { Orders = orders, Total = total, Limit = limit, Skip = skip };
    }


    public async Task<List<OrderDownloadDto>> GetOrderForDownloadAsync(DateTime fromDate, DateTime toDate)
    {
        var orders = await db.Orders
          .Where(x => x.VendorId == tenant.VendorId && x.OrderDate > fromDate.Date && x.OrderDate < toDate.Date.AddDays(1))
           .Include(x => x.Items)
            .Include(x => x.Payment)
            .Select(o => new OrderDownloadDto()
            {
                OrderDate = o.OrderDate,
                BillNumber = o.OrderNumber,
                Total = o.Total,
                Status = o.Status,
                OrderBy = o.MachineId,
                PaymentType = o.Payment == null ? "" : o.Payment.PaymentType,
                OrderItems = string.Join("\r\n", o.Items!.Select(p => $"{p.ProductName} | {p.Price} | {p.Qty} | {Math.Round(p.Price * p.Qty)}").ToList()),
            }).ToListAsync();

        return orders;
    }


    #region Dashboard 

    public async Task<SalesLegendsDto> GetSalesLegends(string machineId = "")
    {
        var date = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var currentDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);

        var query = db.Orders
                   .Where(x => x.VendorId == tenant.VendorId && x.Status == StrDir.OrderStatus.SUCCESS && x.OrderDate >= date)
                   .AsNoTracking();

        if (!string.IsNullOrEmpty(machineId))
            query = query.Where(x => x.MachineId == machineId);

        var sales = await query.Select(x => new { x.OrderDate, x.PaymentType, x.Total }).ToListAsync();
        var todaySales = sales.Where(x => x.OrderDate >= currentDate).ToList();

        return new SalesLegendsDto(
                new SalesTotalDto(
                     sales.Sum(x => x.Total),
                     sales.Where(x => x.PaymentType == StrDir.PaymentType.UPI).Sum(x => x.Total),
                     sales.Where(x => x.PaymentType == StrDir.PaymentType.ACCOUNT).Sum(x => x.Total),
                     sales.Where(x => x.PaymentType == StrDir.PaymentType.CASH).Sum(x => x.Total),
                     sales.Where(x => x.PaymentType == StrDir.PaymentType.CARD).Sum(x => x.Total)
                    ),
                new SalesTotalDto(
                     todaySales.Sum(x => x.Total),
                     todaySales.Where(x => x.PaymentType == StrDir.PaymentType.UPI).Sum(x => x.Total),
                     todaySales.Where(x => x.PaymentType == StrDir.PaymentType.ACCOUNT).Sum(x => x.Total),
                     todaySales.Where(x => x.PaymentType == StrDir.PaymentType.CASH).Sum(x => x.Total),
                     todaySales.Where(x => x.PaymentType == StrDir.PaymentType.CARD).Sum(x => x.Total)
                    )
                );
    }

    public async Task<List<MonthWiseSales>> GetOverAllMonthlySales(string machineId = "")
    {
        DateTime temp = DateTime.Now.AddMonths(-11);
        DateTime startDate = new DateTime(temp.Year, temp.Month, 1);

        var query = db.Orders.Where(x => x.VendorId == tenant.VendorId && x.Status == StrDir.OrderStatus.SUCCESS && x.OrderDate >= startDate);

        if (!string.IsNullOrEmpty(machineId))
            query = query.Where(x => x.MachineId == machineId);


        var sales = await query
                           .Select(x => new { x.OrderDate.Month, x.Total })
                           .GroupBy(x => x.Month)
                           .Select(g => new
                           {
                               Month = g.Key,
                               Total = g.Sum(x => x.Total)
                           }).ToListAsync();

        var MonthWiseSales = Enumerable.Range(0, 12)
                         .Select(e => startDate.AddMonths(e))
                         .Select(e => new MonthWiseSales(
                              e.ToString("MMM"),
                              sales.FirstOrDefault(x => x.Month == e.Month)?.Total ?? 0))
                         .ToList();

        return MonthWiseSales;
    }

    public async Task<List<DailySales>> GetOverAllLastWeekDailySales(string machineId = "")
    {
        DateTime temp = DateTime.Now.AddDays(-6);
        DateTime startDate = new DateTime(temp.Year, temp.Month, temp.Day);

        var query = db.Orders.Where(x => x.VendorId == tenant.VendorId && x.Status == StrDir.OrderStatus.SUCCESS && x.OrderDate >= startDate);
        if (!string.IsNullOrEmpty(machineId))
            query = query.Where(x => x.MachineId == machineId);


        var sales = await query
                           .Select(x => new { x.OrderDate.Day, x.Total })
                           .GroupBy(x => x.Day)
                           .Select(g => new
                           {
                               Day = g.Key,
                               Total = g.Sum(x => x.Total)
                           }).ToListAsync();

        var DailySales = Enumerable.Range(0, 7)
                         .Select(e => startDate.AddDays(e))
                         .Select(e => new DailySales(
                              e.ToString("ddd"),
                              sales.FirstOrDefault(x => x.Day == e.Day)?.Total ?? 0))
                         .ToList();

        return DailySales;
    }

    public async Task<List<ProdcutSalesSumDto>> GetProductSalesForChart(string machineId)
    {
        var date = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var currentDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);

        var orderIds = await db.Orders
                   .Where(x => x.VendorId == tenant.VendorId && x.MachineId == machineId && x.Status == StrDir.OrderStatus.SUCCESS && x.OrderDate >= date)
                   .Select(x => x.Id).ToListAsync();

        var salesItems = await db.OrderItems.Where(x => orderIds.Contains(x.OrderId)).Select(x => new
        {
            Name = x.ProductName + " - " + x.Price,
            x.VendQty
        }).GroupBy(x => x.Name).Select(g => new ProdcutSalesSumDto(g.Key, g.Sum(x => x.VendQty))).ToListAsync();

        return salesItems.Where(x => x.Quantity > 0).ToList() ?? [];
    }

    #endregion


}
