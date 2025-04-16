namespace CMS.Dto;

public record PowerOptionDto(PowerState option);

public enum PowerState
{
    Shutdown,
    Restart
}