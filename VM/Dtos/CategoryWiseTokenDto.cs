namespace VM.Dtos;

public class CategoryWiseTokenDto
{
    public string Category { get; set; } = string.Empty;
    public List<TokenProductDto> Products { get; set; } = [];
}

public record TokenProductDto(string ProductId, string ProductName, int VendQty);