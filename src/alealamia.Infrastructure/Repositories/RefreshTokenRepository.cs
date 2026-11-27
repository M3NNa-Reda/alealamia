using alealamia.Application.Interfaces.Repositories;
using alealamia.Domain.Entities;
using alealamia.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace alealamia.Infrastructure.Repositories
{
    public class RefreshTokenRepository :GenericRepository<RefreshToken>, IRefreshTokenRepository
    {

        public RefreshTokenRepository(ApplicationDbContext context) : base(context)
        {
        }

        public Task<RefreshToken?> GetByTokenHashAsync(
            string tokenHash,
            CancellationToken cancellationToken = default)
        {
            return _context.RefreshTokens
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
        }

        public async Task<bool> TryRevokeForRotationAsync(
            string tokenHash,
            string replacedByTokenHash,
            DateTime nowUtc,
            CancellationToken cancellationToken = default)
        {
            var rows = await _context.RefreshTokens
                .Where(x => x.TokenHash == tokenHash
                            && x.RevokedAt == null
                            && x.ExpiresAt > nowUtc)
                .ExecuteUpdateAsync(
                    s => s
                        .SetProperty(x => x.RevokedAt, (DateTime?)nowUtc)
                        .SetProperty(x => x.ReplacedByTokenHash, replacedByTokenHash),
                    cancellationToken);

            return rows == 1;
        }

        public Task<int> RevokeFamilyAsync(
            Guid tokenFamilyId,
            DateTime nowUtc,
            CancellationToken cancellationToken = default)
        {
            return _context.RefreshTokens
                .Where(x => x.TokenFamilyId == tokenFamilyId && x.RevokedAt == null)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(x => x.RevokedAt, (DateTime?)nowUtc),
                    cancellationToken);
        }

        public Task<int> DeleteExpiredAsync(
            DateTime expiredBeforeUtc,
            CancellationToken cancellationToken = default)
        {
            return _context.RefreshTokens
                .Where(x => x.ExpiresAt < expiredBeforeUtc)
                .ExecuteDeleteAsync(cancellationToken);
        }
        public Task<int> RevokeAllForUserAsync(
            string userId,
            DateTime nowUtc,
            CancellationToken cancellationToken = default)
        {
            return _context.RefreshTokens
                .Where(x => x.UserId == userId && x.RevokedAt == null)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(x => x.RevokedAt, (DateTime?)nowUtc),
                    cancellationToken);
        }
    }
}