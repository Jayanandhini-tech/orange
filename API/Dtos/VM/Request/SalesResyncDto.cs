namespace CMS.API.Dtos.VM.Request;

public record SalesResyncDto(DateTime SalesDate, List<SalesAddDto> SalesRecord);

