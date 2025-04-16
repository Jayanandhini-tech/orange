using CMS.API.Domains;
using CMS.API.Dtos;
using CMS.API.Repository.IRepository;
using CMS.API.Services.Interfaces;
using CMS.Dto.Products;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProductsController : ControllerBase
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IFileStorageService fileStorageService;

        public ProductsController(IUnitOfWork unitOfWork, IFileStorageService fileStorageService)
        {
            this.unitOfWork = unitOfWork;
            this.fileStorageService = fileStorageService;
        }



        [HttpGet]
        public async Task<IActionResult> GetAllProducts()
        {
            var products = await unitOfWork.Products.GetAllAsync();
            return Ok(products.OrderByDescending(x => x.UpdatedOn).Adapt<List<ProductDto>>());
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetProduct(string id)
        {
            var product = await unitOfWork.Products.GetAsync(x => x.Id == id);
            if (product is null)
                return BadRequest("Product not found");

            return Ok(product.Adapt<ProductDto>());
        }


        [HttpGet("recent")]
        public async Task<IActionResult> GetAllRecentProducts(DateTime lastUpdaetdOn)
        {
            var products = await unitOfWork.Products.GetAllAsync(x => x.UpdatedOn > lastUpdaetdOn);
            return Ok(products.OrderBy(x => x.UpdatedOn).Adapt<List<ProductDto>>());
        }


        [HttpPost]
        public async Task<IActionResult> PostProductAsync([FromForm] ProductAdd productAdd)
        {

            string imagePath = fileStorageService.StoreImage(productAdd.ProductImage!, "Products");

            double basePrice = Math.Round((productAdd.Price / (100 + productAdd.GST)) * 100, 2);

            Product product = new Product()
            {
                Id = $"pr_{Guid.NewGuid().ToString("N").ToUpper()}",
                Name = productAdd.Name.Trim(),
                Price = productAdd.Price,
                Gst = productAdd.GST,
                BaseRate = basePrice,
                CategoryId = productAdd.CategoryId,
                ImgPath = imagePath
            };

            await unitOfWork.Products.AddAsync(product);
            await unitOfWork.SaveChangesAsync();

            return Ok(new { Message = "Product created successfully" });
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProductAsync(string id, [FromForm] ProductAdd productUpdate)
        {

            var product = await unitOfWork.Products.GetAsync(x => x.Id == id, tracked: true);
            if (product is null)
                return BadRequest("Product not found");

            product.Name = productUpdate.Name.Trim();
            product.Price = productUpdate.Price;
            product.Gst = productUpdate.GST;
            product.BaseRate = Math.Round((productUpdate.Price / (100 + productUpdate.GST)) * 100, 2);
            product.CategoryId = productUpdate.CategoryId;
            product.ImgPath = productUpdate.ProductImage is not null ? fileStorageService.StoreImage(productUpdate.ProductImage!, "Products") : product.ImgPath;
            await unitOfWork.SaveChangesAsync();

            return Ok(new { Message = "Product updated successfully" });
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(string id)
        {
            var product = await unitOfWork.Products.GetAsync(x => x.Id == id);

            if (product is null)
                return BadRequest("Product not found");

            unitOfWork.Products.Remove(product);
            await unitOfWork.SaveChangesAsync();

            return Ok(new { Message = "Product removed successfully" });
        }

    }
}
