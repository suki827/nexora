using Nexora.Domain.Entities;

namespace Nexora.Api.Services;

public interface IMediaAssetService
{
    Task<List<MediaAsset>> GetAllAsync(
    CancellationToken cancellationToken = default);

    Task<MediaAsset?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<MediaAsset> CreateAsync(
        MediaAsset asset,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(
        MediaAsset asset,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
