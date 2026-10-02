namespace Nexora.Api.Dtos.MediaAssetDtos;

public class MediaAssetDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }

}
