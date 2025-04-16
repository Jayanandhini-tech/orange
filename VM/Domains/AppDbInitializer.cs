using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace VM.Domains;

public class AppDbInitializer
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<AppDbInitializer> logger;

    public AppDbInitializer(IServiceProvider serviceProvider, ILogger<AppDbInitializer> logger)
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;
    }

    public async Task Initialize()
    {
        //migrations if they are not applied
        try
        {
            IServiceScope scope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();

            AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (dbContext.Database.GetPendingMigrations().Count() > 0)
            {
                await dbContext.Database.MigrateAsync();
            }

        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }

        return;
    }
}
