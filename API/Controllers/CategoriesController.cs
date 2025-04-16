using CMS.API.Domains;
using CMS.API.Dtos.Categories;
using CMS.API.Repository.IRepository;
using CMS.API.Services.Interfaces;
using CMS.Dto;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;

namespace CMS.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly IUnitOfWork unitOfWork;
    private readonly IFileStorageService fileStorageService;

    public CategoriesController(IUnitOfWork unitOfWork, IFileStorageService fileStorageService)
    {
        this.unitOfWork = unitOfWork;
        this.fileStorageService = fileStorageService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllCategories()
    {
        var categories = await unitOfWork.Categories.GetAllAsync();
        return Ok(categories.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name).Adapt<List<CategoryDto>>());
    }


    [HttpGet("{id}")]
    public async Task<IActionResult> GetCategory(string id)
    {
        var category = await unitOfWork.Categories.GetAsync(x => x.Id == id);
        if (category is null)
            return BadRequest("Category not found");

        return Ok(category.Adapt<CategoryDto>());
    }

    [HttpPost]
    public async Task<IActionResult> AddCategory(CategoryAdd addDto)
    {
        bool exist = await unitOfWork.Categories.AnyAsync(x => x.Name == addDto.Name.Trim());
        if (exist)
            return BadRequest($"Category {addDto.Name} already exist");

        string imagePath = fileStorageService.StoreImage(addDto.Image, new Size(200, 200), "Categories");

        Category category = new Category()
        {
            Id = $"ca_{Guid.NewGuid().ToString("N").ToUpper()}",
            Name = addDto.Name.Trim(),
            ImgPath = imagePath
        };

        await unitOfWork.Categories.AddAsync(category);
        await unitOfWork.SaveChangesAsync();

        return Ok(new { Message = "Category created successfully" });
    }


    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCategoryAsync(string id, [FromForm] CategoryAdd categoryUpdate)
    {

        var category = await unitOfWork.Categories.GetAsync(x => x.Id == id, tracked: true);
        if (category is null)
            return BadRequest("Category not found");

        category.Name = categoryUpdate.Name.Trim();
        category.DisplayOrder = categoryUpdate.DisplayOrder;
        category.ImgPath = categoryUpdate.Image is not null ? fileStorageService.StoreImage(categoryUpdate.Image!, new Size(200, 200), "Categories") : category.ImgPath;
        await unitOfWork.SaveChangesAsync();

        return Ok(new { Message = "Category updated successfully" });
    }


    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(string id)
    {
        var category = await unitOfWork.Categories.GetAsync(x => x.Id == id);

        if (category is null)
            return BadRequest("Category not found");

        unitOfWork.Categories.Remove(category);
        await unitOfWork.SaveChangesAsync();

        return Ok(new { Message = "Category removed successfully" });
    }

}
