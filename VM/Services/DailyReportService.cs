using Microsoft.Extensions.DependencyInjection;
using VM.Services.Interfaces;

namespace VM.Services;

public class DailyReportService : IDailyReportService
{
    private Timer? _timer;

    private readonly IServiceScopeFactory serviceScopeFactory;

    public DailyReportService(IServiceScopeFactory serviceScopeFactory)
    {
        this.serviceScopeFactory = serviceScopeFactory;
    }

    public async void Start()
    {
        TimeSpan delay = CalculateDelayUntil1AM();
        _timer = new Timer(DoWork, null, delay, TimeSpan.FromHours(24));

        var scope = serviceScopeFactory.CreateScope();
        var reportService = scope.ServiceProvider.GetRequiredService<IReportService>();
        await reportService.SendTodayAutomatedReport();
    }

    private TimeSpan CalculateDelayUntil1AM()
    {
        DateTime now = DateTime.Now;
        DateTime targetTime = new DateTime(now.Year, now.Month, now.Day, 1, 0, 0);
        if (now.Hour >= 1)
            targetTime = targetTime.AddDays(1);

        return targetTime - now;
    }

    private async void DoWork(object? state)
    {
        var scope = serviceScopeFactory.CreateScope();
        var reportService = scope.ServiceProvider.GetRequiredService<IReportService>();
        await reportService.SendTodayAutomatedReport();
    }

    public void Stop()
    {
        _timer?.Dispose();
    }
}
