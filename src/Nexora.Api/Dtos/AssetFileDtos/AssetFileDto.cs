namespace Nexora.Api.Dtos.AssetFileDtos;

public class AssetFileDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }

}
