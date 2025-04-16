using CMS.API.Domains;
using CMS.API.Repository.IRepository;
using CMS.API.Services.Interfaces;
using CMS.Dto;
using Microsoft.EntityFrameworkCore;

namespace CMS.API.Repository;

public class StockMachineRepository : Repository<StockMachine>, IStockMachineRepository
{
    private readonly AppDBContext db;
    private readonly ITenant tenant;

    public StockMachineRepository(AppDBContext db, ITenant tenant) : base(db, tenant)
    {
        this.db = db;
        this.tenant = tenant;
    }

    public async Task<StockDisplayDto> GetMachineStocksAsync(string machineId)
    {
        var stocks = await db.StockMachines
                        .Include(x => x.Product)
                        .Where(x => x.VendorId == tenant.VendorId && x.IsActive == true && x.MachineId == machineId)
                        .Select(x => new
                        {
                            x.CabinId,
                            x.MotorNumber,
                            Name = x.Product == null ? "" : x.Product.Name,
                            Price = x.Product == null ? 0 : x.Product.Price,
                            ImagePath = x.Product == null ? "" : x.Product.ImgPath,
                            x.Capacity,
                            x.Stock,
                            x.Soldout
                        })
                        .ToListAsync();

        var cabin1 = stocks.Where(x => x.CabinId == 1).Select(x => new StockItemDto(x.MotorNumber, x.Name, x.Price, x.ImagePath, x.Capacity, x.Stock, x.Soldout)).ToList();
        var cabin2 = stocks.Where(x => x.CabinId == 2).Select(x => new StockItemDto(x.MotorNumber, x.Name, x.Price, x.ImagePath, x.Capacity, x.Stock, x.Soldout)).ToList();

        List<CabinRowDto> cabin1Rows = [];
        List<CabinRowDto> cabin2Rows = [];

        for (int i = 0; i <= 100; i += 10)
        {
            var rowc1 = cabin1.Where(x => x.MotorNumber > i && x.MotorNumber <= (i + 10)).OrderBy(x => x.MotorNumber).ToList();
            if (rowc1.Count > 0)
                cabin1Rows.Add(new CabinRowDto(rowc1));

            if (cabin2.Count > 0)
            {
                var rowc2 = cabin2.Where(x => x.MotorNumber > i && x.MotorNumber <= (i + 10)).OrderBy(x => x.MotorNumber).ToList();
                if (rowc2.Count > 0)
                    cabin2Rows.Add(new CabinRowDto(rowc2));
            }
        }


        return new StockDisplayDto(cabin1Rows, cabin2Rows);
    }

    public async Task<List<StockReportDto>> GetMachineStocksByProductAsync(string machineId)
    {

        var query = db.StockMachines
                       .Include(x => x.Product)
                       .Where(x => x.VendorId == tenant.VendorId && x.IsActive == true && !string.IsNullOrEmpty(x.ProductId));

        if (!string.IsNullOrEmpty(machineId))
            query = query.Where(x => x.MachineId == machineId);


        var stocks = await query.Select(x =>
                                new
                                {
                                    x.ProductId,
                                    Name = x.Product == null ? "" : x.Product.Name,
                                    Price = x.Product == null ? 0 : x.Product.Price,
                                    x.Capacity,
                                    x.Stock
                                })
                               .GroupBy(x => x.ProductId)
                               .Select(g =>
                                    new StockReportDto(
                                   g.First().Name,
                                   g.First().Price,
                                   g.Sum(x => x.Capacity),
                                   g.Sum(x => x.Stock),
                                   g.Sum(x => x.Stock * x.Price)
                                   )).ToListAsync();

        return stocks.OrderBy(x => x.Product).ToList() ?? [];
    }


}
