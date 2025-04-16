using CMS.API.Domains;
using CMS.Dto.Products;
using Mapster;

namespace CMS.API.Services;

public static class MapsterConfig
{
    public static void ConfigureMapster()
    {
        TypeAdapterConfig<Product, ProductDto>.NewConfig().Map( dest => dest.gst , src => src.Gst);
        //TypeAdapterConfig<Category, KioskCategoryProducts>
        //    .NewConfig()
        //    .Map(d => d.CategoryId, s => s.Id)
        //    .Map(d => d.CategoryName, s => s.Name);
    }
}
