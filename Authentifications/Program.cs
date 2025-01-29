using System.Net.Security;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Authentifications.Interfaces;
using Authentifications.Middlewares;
using Authentifications.RedisContext;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using Hangfire;
using Hangfire.Redis.StackExchange;
using Hangfire.Dashboard.BasicAuthorization;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Authentifications.Services;

var builder = WebApplication.CreateBuilder(args);
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.IsEssential = true;
});
builder.Services.AddControllers();
builder.Services.Configure<ApiBehaviorOptions>(options => options.SuppressModelStateInvalidFilter = true);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc("v1.0", new OpenApiInfo
    {
        Title = "Authentification service | Api",
        Description = "An ASP.NET Core Web API for managing Users authentification",
        Version = "v1.0",
        Contact = new OpenApiContact
        {
            Name = "Artur Lambo",
            Email = "lamboartur94@gmail.com"
        }
    });

    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    opt.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                      });
});

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: false, reloadOnChange: false);

builder.Services.AddHttpClient();
builder.Services.AddControllersWithViews();
builder.Services.AddRouting();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDataProtection();
builder.Services.AddHealthChecks();
builder.Logging.AddConsole();

var kestrelSectionCertificate = builder.Configuration.GetSection("Kestrel:EndPoints:Https:Certificate");
var certificateFile = kestrelSectionCertificate["File"];
var certificatePassword = kestrelSectionCertificate["Password"];

builder.Services.Configure<KestrelServerOptions>(options =>
{
    if (string.IsNullOrEmpty(certificateFile) || string.IsNullOrEmpty(certificatePassword))
    {
        throw new InvalidOperationException("Certificate path or password not configured");
    }
    options.Limits.MaxConcurrentConnections = 100;
    options.Limits.MaxRequestBodySize = 10 * 1024;
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
    options.ConfigureHttpsDefaults(opt =>
    {
        opt.ClientCertificateMode = ClientCertificateMode.NoCertificate; // Required Certificate dans les autres services c'est du allowCertificate
    });
});

builder.Services.AddScoped<IJwtAccessAndRefreshTokenService, JwtAccessAndRefreshTokenService>();
builder.Services.AddScoped<IRedisCacheService, RedisCacheService>();
builder.Services.AddScoped<IRedisCacheTokenService, RedisCacheTokenService>();

builder.Services.AddScoped<JwtAccessAndRefreshTokenService>();
builder.Services.AddTransient<AuthentificationBasicMiddleware>();

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
builder.Services.AddLogging();

builder.Services.AddAuthorization();
builder.Services.AddAuthentication("BasicAuthentication")
    .AddScheme<AuthenticationSchemeOptions, AuthentificationBasicMiddleware>("BasicAuthentication", _ => { });

var Config = builder.Configuration.GetSection("Redis");

var clientCertificate = new X509Certificate2(
    Config["Certificate:Redis-pfx"], // Chemin du certificat client
    Config["Certificate:Pfx-password"], // Mot de passe du certificat
    X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.MachineKeySet
);

var options = new ConfigurationOptions
{
    EndPoints = { Config["ConnectionString"] }, //par "localhost:6379"
    Ssl = true, // Activation de TLS obligatoire
    SslHost = "Redis-server", // Nom d'hôte à valider dans le certificat
    Password = Config["Password"], // Mot de passe Redis
    AbortOnConnectFail = false,
    SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13, // Limité à TLS 1.2 et 1.3 comme sur le serveur
    AllowAdmin = true,
    ConnectTimeout = 10000,
    SyncTimeout = 10000,
    ReconnectRetryPolicy = new ExponentialRetry(5000)
};

// Validation du certificat serveur
options.CertificateValidation += (sender, certificate, chain, sslPolicyErrors) =>
{
    // Accepter uniquement si le certificat est valide ou s'il est signé par Redis-CA
    return sslPolicyErrors == SslPolicyErrors.None ||
           (sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors &&
            chain!.ChainElements[^1].Certificate.Subject == "CN=Redis-CA");
};

options.CertificateSelection += delegate { return clientCertificate; };

builder.Services.AddStackExchangeRedisCache(opts => opts.ConfigurationOptions = options);
builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
{
    try
    {
        var multiplexer = ConnectionMultiplexer.Connect(options);
        return multiplexer;
    }
    catch (RedisConnectionException  ex)
    {
        var logger = provider.GetRequiredService<ILogger<Program>>();
        logger.LogCritical(ex,"Error connecting to Redis:");
        throw;
    }
});

builder.Services.AddHangfire((serviceProvider, config) =>
{
    var multiplexer = serviceProvider.GetRequiredService<IConnectionMultiplexer>();
    config.UseRedisStorage(multiplexer);
});

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 5;
    options.SchedulePollingInterval = TimeSpan.FromMinutes(3);
    options.Queues = new[] { "forcast_task" };
});

var app = builder.Build();
var HangFireConfig = builder.Configuration.GetSection("HangfireCredentials");
app.UseHangfireDashboard("/lambo-authentication-manager/hangfire", new DashboardOptions()
{
    DashboardTitle = "Hangfire Dashboard for Lamboft Inc ",
    Authorization = new[]
    {
        new BasicAuthAuthorizationFilter(
            new BasicAuthAuthorizationFilterOptions
            {
                Users = new[]
                {
                    new BasicAuthAuthorizationUser
                    {
                        Login = HangFireConfig["UserName"],
                        PasswordClear = HangFireConfig["Password"]
                    }
                }
            })
    }
});

app.Lifetime.ApplicationStarted.Register(() =>
{
    BackgroundJob.Schedule<RedisCacheService>( //Producer
        "call_api",
        service => service.BackGroundJob(),
        TimeSpan.Zero  // On initie immédiatement la tâche
    );
    // BackgroundJob.Schedule<RedisCacheService>(
    // 	"delete_cache", // Identifiant unique de la tâche
    // 	service => service.DeleteRedisCacheAfterOneDay(),
    // 	TimeSpan.Zero 
    // );
});
app.UseMiddleware<ContextPathMiddleware>("/lambo-authentication-manager");
if (app.Environment.IsProduction())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseSwagger();
    app.UseSwaggerUI(con =>
    {
         con.SwaggerEndpoint("/lambo-authentication-manager/swagger/v1.0/swagger.yml", "Gestion des authentification");

         con.RoutePrefix = string.Empty;
    });
}
app.UseCors(MyAllowSpecificOrigins);
app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapHealthChecks("/health");
    endpoints.MapGet("/version", async context => await context.Response.WriteAsync("Version de l'API : v1.0"));
});
await app.RunAsync();