using alealamia.Domain.Entities;

namespace alealamia.Application.Interfaces.Repositories
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByTokenHashAsync(
            string tokenHash,
            CancellationToken cancellationToken = default);

        Task<bool> TryRevokeForRotationAsync(
            string tokenHash,
            string replacedByTokenHash,
            DateTime nowUtc,
            CancellationToken cancellationToken = default);

        Task<int> RevokeFamilyAsync(
            Guid tokenFamilyId,
            DateTime nowUtc,
            CancellationToken cancellationToken = default);

        Task<int> DeleteExpiredAsync(
            DateTime expiredBeforeUtc,
            CancellationToken cancellationToken = default);
    }
}