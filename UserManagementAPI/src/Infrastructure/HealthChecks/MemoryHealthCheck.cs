using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Infrastructure.HealthChecks;

/// <summary>
/// Health check for monitoring memory usage
/// </summary>
public class MemoryHealthCheck : IHealthCheck
{
    private readonly long _threshold;

    public MemoryHealthCheck(long threshold = 1024 * 1024 * 1024) // 1GB default
    {
        _threshold = threshold;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentMemory = GC.GetTotalMemory(false);
            var data = new Dictionary<string, object>
            {
                ["CurrentMemoryUsage"] = currentMemory,
                ["ThresholdMemory"] = _threshold,
                ["MemoryUsageMB"] = Math.Round(currentMemory / 1024.0 / 1024.0, 2)
            };

            var status = currentMemory > _threshold ? HealthStatus.Degraded : HealthStatus.Healthy;
            var description = $"Memory usage: {Math.Round(currentMemory / 1024.0 / 1024.0, 2)} MB";

            return Task.FromResult(new HealthCheckResult(status, description, null, data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new HealthCheckResult(HealthStatus.Unhealthy, "Memory check failed", ex));
        }
    }
}
