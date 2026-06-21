using TripPacking.Services;

namespace TripPacking.BackgroundServices;

public class InvitationCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InvitationCleanupService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);

    public InvitationCleanupService(IServiceProvider serviceProvider, ILogger<InvitationCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Invitation cleanup background service is starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var invitationService = scope.ServiceProvider.GetRequiredService<IInvitationService>();
                    var expiredCount = await invitationService.ExpireInvitationsAsync();
                    if (expiredCount > 0)
                    {
                        _logger.LogInformation("Cleaned up {Count} expired invitations", expiredCount);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while cleaning up expired invitations");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Invitation cleanup background service is stopping");
    }
}
