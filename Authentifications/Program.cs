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

// Configuration de la session
builder.Services.AddSession(options =>
{
	options.IdleTimeout = TimeSpan.FromMinutes(30); // Durée de vie de la session
	options.Cookie.HttpOnly = true; // Protéger contre les scripts malveillants (XSS)
	options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // HTTPS obligatoire
	options.Cookie.SameSite = SameSiteMode.Strict; // CSRF protection
	options.Cookie.IsEssential = true; // Nécessaire pour activer les sessions
});
builder.Services.AddControllers();


builder.Services.Configure<ApiBehaviorOptions>(options =>
{
	options.SuppressModelStateInvalidFilter = true;
});

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

/*
	+----------------------------------------------------------------------+
	|Enregistrement de services Injectées lorsqu'une interface est démandée|
	+----------------------------------------------------------------------+
*/
builder.Services.AddScoped<IJwtAccessAndRefreshTokenService, JwtAccessAndRefreshTokenService>();
builder.Services.AddScoped<IRedisCacheService, RedisCacheService>();
builder.Services.AddScoped<IRedisCacheTokenService, RedisCacheTokenService>();

/* 
	+----------------------------------------------------+
	| Enregistrement de repositories/Services Injectés directement|
	+----------------------------------------------------+
*/

builder.Services.AddScoped<JwtAccessAndRefreshTokenService>();
builder.Services.AddTransient<AuthentificationBasicMiddleware>();


builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
builder.Services.AddLogging();

builder.Services.AddAuthorization();
builder.Services.AddAuthentication("BasicAuthentication")
	.AddScheme<AuthenticationSchemeOptions, AuthentificationBasicMiddleware>("BasicAuthentication", options => { });

var Config = builder.Configuration.GetSection("Redis");
var clientCertificate = new X509Certificate2(
	Config["Certificate:Redis-pfx"],
	Config["Certificate:Pfx-password"],
	X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.MachineKeySet
);
var options = new ConfigurationOptions
{
	EndPoints = { Config["ConnectionString"] },
	Ssl = true,
	SslHost = "Redis-Server", // Nom d'hôte TLS (C'est le common name du certificat pour le server redis pas pour le client)
	Password = Config["Password"], // Mot de passe Redis
	AbortOnConnectFail = false,
	SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13, // Vérifier la version tls de redis en amont
	AllowAdmin = true,
	ConnectTimeout = 10000, // Augmenter le délai de connexion
	SyncTimeout = 10000,
	ReconnectRetryPolicy = new ExponentialRetry(5000)
};
// On charge le CA
var caCertificate = new X509Certificate2(Config["Certificate:Redis-ca"]);
// Ajouter le certificat CA à la validation
options.CertificateValidation += (sender, certificate, chain, sslPolicyErrors) =>
{
	if (sslPolicyErrors == SslPolicyErrors.None)
		return true;

	// Accepter uniquement les erreurs liées à une CA auto-signée
	if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors && chain!.ChainElements.Count > 1)
	{
		// Vérifiez si le certificat racine est Redis-CA
		var rootCert = chain.ChainElements[^1].Certificate;
		return rootCert.Subject == "CN=Redis-CA";
	}

	return false;
};

// Spécifier le certificat client
options.CertificateSelection += delegate { return clientCertificate; };
// Configurer le cache distribué avec StackExchange.Redis
builder.Services.AddStackExchangeRedisCache(opts =>
{
	opts.ConfigurationOptions = options;

});
// Ajouter IConnectionMultiplexer une seule fois
builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
{
	try
	{
		var multiplexer = ConnectionMultiplexer.Connect(options);
		return multiplexer;
	}
	catch (Exception ex)
	{
		var logger = provider.GetRequiredService<ILogger<Program>>();
		logger.LogCritical("Error connecting to Redis: {ex.Message}", ex.Message);
		throw;
	};
});

builder.Services.AddHangfire((serviceProvider, config) =>
{
	var multiplexer = serviceProvider.GetRequiredService<IConnectionMultiplexer>();
	config.UseRedisStorage(multiplexer);
});

// consumer 
builder.Services.AddHangfireServer(options =>
{
	options.WorkerCount = 5;
	options.SchedulePollingInterval = TimeSpan.FromMinutes(3); // Vérifier toutes les 10 secondes
	options.Queues = new[] { "forcast_task" };

});

var app = builder.Build();
// Ajouter le tableau de bord et le serveur Hangfire
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
		"call_api", // Identifiant unique de la tâche
		service => service.BackGroundJob(),
		TimeSpan.Zero  // On initie immédiatement la tâche
	);
	// BackgroundJob.Schedule<RedisCacheService>(
	// 	"delete_cache", // Identifiant unique de la tâche
	// 	service => service.DeleteRedisCacheAfterOneDay(),
	// 	TimeSpan.Zero 
	// );
});
/* 
	+----------------------------------------------------+
	| Enregistrement de middlewares Injection directe	 |
	+----------------------------------------------------+
*/

app.UseMiddleware<ContextPathMiddleware>("/lambo-authentication-manager");
//app.UseMiddleware<ValidationHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI(con =>
	 {
		 con.SwaggerEndpoint("/lambo-authentication-manager/swagger/v1.0/swagger.yml", "Gestion des authentification"); // dépend de la version définie dans le swaggerDoc

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
		await context.Response.WriteAsync("Version de l'API : v1.0");
	});
});

app.Run();
