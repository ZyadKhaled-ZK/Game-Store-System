using GameStore.DAL.DataBase;
using Microsoft.EntityFrameworkCore;

namespace GameStore.PL.Services
{
    public class TokenCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TokenCleanupService> _logger;

        public TokenCleanupService(IServiceScopeFactory scopeFactory, ILogger<TokenCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<GameStoreDbContext>();

                    var cutoff = DateTime.UtcNow.AddDays(-30);
                    var deleted = await db.EmailVerificationTokens
                        .Where(t => t.IsUsed || t.ExpiresAt < cutoff)
                        .ExecuteDeleteAsync(stoppingToken);

                    if (deleted > 0)
                        _logger.LogInformation("Cleaned up {Count} expired/used email verification tokens", deleted);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Email verification token cleanup failed");
                }

                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}
