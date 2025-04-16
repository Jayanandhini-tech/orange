using CMS.API.Domains;
using CMS.API.Domains.DbInitializer;
using CMS.API.Hubs;
using CMS.API.Middlewares;
using CMS.API.Payments.Dtos.Gateway;
using CMS.API.Payments.Services;
using CMS.API.Payments.Services.Interface;
using CMS.API.Repository;
using CMS.API.Repository.IRepository;
using CMS.API.Services;
using CMS.API.Services.Interfaces;
using Mapster;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Pomelo.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Logging.AddLog4Net();
builder.Services.AddCors(x => x.AddDefaultPolicy(p => { p.AllowAnyOrigin().AllowAnyMethod().AllowCredentials().AllowAnyHeader().SetIsOriginAllowed(x => true).WithOrigins("http://*:8100"); }));
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();

builder.Services.AddDbContext<AppDBContext>(options =>
{
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection")));
});

builder.Services.AddIdentity<IdentityUser, IdentityRole>().AddEntityFrameworkStores<AppDBContext>().AddDefaultTokenProviders();

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(o =>
{

    o.SaveToken = true;
    byte[] Key = Encoding.UTF8.GetBytes(builder.Configuration["JWT:Key"]!);
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false, // on production make it true
        ValidateAudience = false, // on production make it true
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JWT:Issuer"],
        ValidAudience = builder.Configuration["JWT:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Key),
        ClockSkew = TimeSpan.Zero
    };
    o.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
            {
                context.Response.Headers.Append("IS-TOKEN-EXPIRED", "true");
            }
            return Task.CompletedTask;
        },
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && (path.StartsWithSegments("/hubs")))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});


builder.Services.AddScoped<IDbInitializer, DbInitializer>();
builder.Services.AddScoped<ITokenProvider, TokenProvider>();
builder.Services.AddScoped<ITenant, Tenant>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddSingleton<IFileStorageService, FileStorageService>();


builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IPendingMessageService, PendingMessageService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// PaymentServices
builder.Services.AddScoped<IDynamicQRCodeGenerator, DynamicQRCodeGenerator>();
builder.Services.AddScoped<IPaymentCallbackService, PaymentCallbackService>();
builder.Services.AddScoped((serviceProvider) => CreatePaymentService(serviceProvider));

var app = builder.Build();

app.RegisterFileProviders();
app.UseMiddleware<ExceptionMiddleware>();

await Seed();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "BVC CMS Backend");
app.MapGet("/api", () => "BVC CMS Backend");
app.MapHub<NotificationHub>("/hubs/notification", options =>
{
    options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets;
});
app.MapControllers();

MapsterConfig.ConfigureMapster();

app.Run();








async Task Seed()
{

    using (var scope = app.Services.CreateScope())
    {
        var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
        await dbInitializer.Initialize();
    }
}


IPaymentService CreatePaymentService(IServiceProvider serviceProvider)
{
    var tenant = serviceProvider.GetRequiredService<ITenant>();
    var dbContext = serviceProvider.GetRequiredService<AppDBContext>();
    var machine = dbContext.Machines.Where(x => x.Id == tenant.MachineId && x.VendorId == tenant.VendorId).FirstOrDefault();

    PgSetting? setting = machine is null || machine.PgSettingId == 0 ?
                             dbContext.PgSettings.FirstOrDefault(x => x.VendorId == tenant.VendorId) :
                             dbContext.PgSettings.Find(machine.PgSettingId);

    if (setting is null)
        setting = dbContext.PgSettings.FirstOrDefault();

    var config = setting.Adapt<PaymentGatewayConfigDto>();

    return setting!.GatewayName switch
    {
        "PhonePe" => new PhonepeService(config,
                                        serviceProvider.GetRequiredService<HttpClient>(),
                                        serviceProvider.GetRequiredService<IHttpContextAccessor>(),
                                        serviceProvider.GetRequiredService<ILogger<PhonepeService>>()),

        "ICICI" => new IciciService(config,
                                    serviceProvider.GetRequiredService<HttpClient>(),
                                    serviceProvider.GetRequiredService<ILogger<IciciService>>()),


        "HDFC" => new HDFCService(config,
                                  serviceProvider.GetRequiredService<HttpClient>(),
                                  serviceProvider.GetRequiredService<ILogger<HDFCService>>()),

        _ => new PhonepeService(config,
                                serviceProvider.GetRequiredService<HttpClient>(),
                                serviceProvider.GetRequiredService<IHttpContextAccessor>(),
                                serviceProvider.GetRequiredService<ILogger<PhonepeService>>())
    };

}