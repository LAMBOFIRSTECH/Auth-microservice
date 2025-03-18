using System.Net.Security;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Authentifications.Core.Interfaces;
using Authentifications.Application.Middlewares;
using Authentifications.Infrastructure.RedisContext;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using Hangfire;
using Hangfire.Redis.StackExchange;
using Hangfire.Dashboard.BasicAuthorization;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Authentifications.Infrastructure.InternalServices;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using Authentifications.Infrastructure.OpenLdap;

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
builder.Configuration
    .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "Infrastructure/Configurations"))
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: false, reloadOnChange: false)
    .AddEnvironmentVariables();
foreach (var kvp in builder.Configuration.AsEnumerable())
{
    if (kvp.Value?.StartsWith("${") == true && kvp.Value.EndsWith("}"))
    {
        var envVarName = kvp.Value.Trim(new char[] { '$', '{', '}' });
        var envValue = Environment.GetEnvironmentVariable(envVarName);
        if (!string.IsNullOrEmpty(envValue))
        {
            builder.Configuration[kvp.Key] = envValue;
        }
    }
}

builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc(builder.Configuration["Kestrel:ApiVersion"], new OpenApiInfo
    {
        Title = "Authentification service | Api",
        Description = "An ASP.NET Core Web API for managing Users authentification",
        Version = builder.Configuration["Kestrel:ApiVersion"],
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

builder.Services.AddHttpClient();
builder.Services.AddControllersWithViews();
builder.Services.AddRouting();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDataProtection();
builder.Services.AddHealthChecks();
builder.Logging.AddConsole();
var kestrelSectionCertificate = builder.Configuration.GetSection("Kestrel:EndPoints:Https:Certificate");
var certificateFile = kestrelSectionCertificate["File"];
var certificatePassword = kestrelSectionCertificate["KESTREL_PASSWORD"];

builder.Services.Configure<KestrelServerOptions>(options =>
{
    if (string.IsNullOrEmpty(certificateFile) || string.IsNullOrEmpty(certificatePassword))
    {
        throw new InvalidOperationException("Certificate path or password not configured");
    }
    options.Limits.MaxConcurrentConnections = 100;
    options.Limits.MaxRequestBodySize = 10 * 1024;
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
    options.ConfigureHttpsDefaults(opt => opt.ClientCertificateMode = ClientCertificateMode.NoCertificate);
});
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("api-authentication"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddJaegerExporter(jaegerOptions =>
            {
                jaegerOptions.AgentHost = builder.Configuration["Jaeger:IpAddress"];
                jaegerOptions.AgentPort = Int16.Parse(builder.Configuration["Jaeger:Port"]!);
            });
    });

builder.Services.AddSingleton<IJwtAccessAndRefreshTokenService, JwtAccessAndRefreshTokenService>();
builder.Services.AddSingleton<IRedisCacheService, RedisCacheService>();
builder.Services.AddSingleton<IRedisCacheTokenService, RedisCacheTokenService>();
builder.Services.AddSingleton<IHashicorpVaultService, HashicorpVaultService>();
builder.Services.AddSingleton<IRabbitMqService, RabbitMqService>();
builder.Services.AddSingleton<IHangFireService, HangFireService>();
builder.Services.AddSingleton<ILdapService, LdapService>();

builder.Services.AddScoped<JwtAccessAndRefreshTokenService>();
builder.Services.AddTransient<AuthentificationBasicMiddleware>();

builder.Services.AddHostedService<RabbitListenerService>();
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
builder.Services.AddLogging();

builder.Services.AddAuthorization();
builder.Services.AddAuthentication("BasicAuthentication")
    .AddScheme<AuthenticationSchemeOptions, AuthentificationBasicMiddleware>("BasicAuthentication", _ => { });

var Config = builder.Configuration.GetSection("Redis");

var clientCertificate = new X509Certificate2(
    Config["Certificate:Redis-pfx"]!,
    Config["Certificate:REDIS_PFX_PASSWORD"],
    X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.EphemeralKeySet
);

var options = new ConfigurationOptions
{
    EndPoints = { Config["ConnectionString"]! },
    Ssl = true,
    SslHost = "Redis-server",
    Password = Config["REDIS_PASSWORD"],
    AbortOnConnectFail = false,
    SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
    AllowAdmin = true,
    ConnectTimeout = 10000,
    SyncTimeout = 10000,
    ReconnectRetryPolicy = new ExponentialRetry(5000)
};

options.CertificateValidation += (__, _, chain, sslPolicyErrors) =>
{
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
        return ConnectionMultiplexer.Connect(options);
    }
    catch (RedisConnectionException ex)
    {
        var logger = provider.GetRequiredService<ILogger<Program>>();
        logger.LogCritical(ex, "Error connecting to Redis:");
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
    options.WorkerCount = 3;
    options.Queues = new[] { "default","runner_operation_between_openldap_and_redis"};
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
                        PasswordClear = HangFireConfig["HANGFIRE_PASSWORD"]
                    }
                }
            })
    }
});
app.UseMiddleware<ContextPathMiddleware>("/lambo-authentication-manager");
if (app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Debug");
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
    endpoints.MapGet("/version", async context =>
    {
        var version = app.Configuration.GetValue<string>("Kestrel:ApiVersion") ?? "Version not set";
        await context.Response.WriteAsync(version);
    });
});
await app.RunAsync();