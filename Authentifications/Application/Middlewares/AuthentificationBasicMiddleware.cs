using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Authentifications.Core.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
namespace Authentifications.Application.Middlewares;
public class AuthentificationBasicMiddleware : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly ILogger<AuthentificationBasicMiddleware> log;
    private readonly IRedisCacheService redisCache;
    public AuthentificationBasicMiddleware(IRedisCacheService redisCache, IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock, ILogger<AuthentificationBasicMiddleware> log)
    : base(options, logger, encoder, clock)
    {
        this.log = log;
        this.redisCache = redisCache;
    }
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        log.LogInformation("En-têtes de la requête : {Headers}", string.Join(", ", Request.Headers.Select(h => $"{h.Key}: {h.Value}")));

        if (!Request.Headers.ContainsKey("Authorization"))
            return AuthenticateResult.Fail("Authorization header missing");
        try
        {
            var authHeader = AuthenticationHeaderValue.Parse(Request.Headers["Authorization"]);
            if (string.IsNullOrEmpty(authHeader.Parameter) || !authHeader.Scheme.Equals("Basic", StringComparison.OrdinalIgnoreCase))
                return AuthenticateResult.Fail("Invalid Authorization header format");

            var credentialBytes = Convert.FromBase64String(authHeader.Parameter!);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':', 2);
            if (credentials.Length != 2)
                return AuthenticateResult.Fail("Invalid Authorization header format");

            var email = credentials[0];
            var password = credentials[1];
            Context.Items["password"] = password;
            Context.Items["email"] = email;
            const string regexMatch = "(?<alpha>\\w+)@(?<mailing>[aA-zZ]+)\\.(?<domaine>[aA-zZ]+$)";
            Match check = Regex.Match(email, regexMatch);
            if (!check.Success)
                return AuthenticateResult.Fail($"The following email address is invalid => {email}");

            if (!await ValidateCredentials(email, password))
            {
                log.LogWarning("Invalid credentials");
                Context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return AuthenticateResult.NoResult();
            }
            var claims = new List<Claim> { new(ClaimTypes.Email, email) };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return AuthenticateResult.Success(ticket);
        }
        catch (FormatException)
        {
            return AuthenticateResult.Fail("Invalid Authorization header encoding");
        }
        catch (Exception ex)
        {
            return AuthenticateResult.Fail($"Authentication failed: {ex.Message}");
        }
    }
    public async Task<bool> ValidateCredentials(string email, string password)
    {
        return (await redisCache.GetBooleanAndUserDataFromRedisUsingParamsAsync(true, email, password)).Item1;
    }
}