using Microsoft.AspNetCore.Mvc;
using Authentifications.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Authentifications.Models;
namespace Authentifications.Controllers;
[Route("auth")]
public class TokenController : ControllerBase
{
    private readonly IJwtAccessAndRefreshTokenService jwtToken;
    private readonly IRedisCacheTokenService redisTokenCache;
    private readonly IRedisCacheService redisCache;
    private readonly ILogger<TokenController> log;
    private string? email;
    private string? password;
    public TokenController(ILogger<TokenController> log, IRedisCacheTokenService redisTokenCache, IJwtAccessAndRefreshTokenService jwtToken, IRedisCacheService redisCache)
    {
        this.jwtToken = jwtToken;
        this.log = log;
        this.redisTokenCache = redisTokenCache;
        this.redisCache = redisCache;

    }
    /// <summary>
    /// Authentifie un utilisateur et retourne les tokens (access et refresh).
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult> Authentificate()
    {
        email = HttpContext.Items["email"] as string;
        password = HttpContext.Items["password"] as string;
        // var ticket = new AuthenticationTicket(User, "Basic");
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return BadRequest("Email or password is missing.");
        if (!User.Identity!.IsAuthenticated)
            return Unauthorized("Unauthorized access");
        var user = await jwtToken.AuthUserDetailsAsync((User.Identity!.IsAuthenticated, email, password));
        var result = jwtToken.GetToken(user);
        if (!result.Response)
            return Unauthorized(new TokenResult
            {
                Response = result.Response,
                Message = result.Message,
                Token = null,
                RefreshToken = null
            });
        HttpContext.Session.SetString("email", email);
        HttpContext.Session.SetString("password", password);
        var tokenResult = new TokenResult()
        {
            Response = result.Response,
            Message = "AccessToken and refreshToken have been successfully generated ",
            Token = result.Token,
            RefreshToken = result.RefreshToken
        };
        return CreatedAtAction(nameof(Authentificate), new { tokenResult });
    }
    /// <summary>
    /// Rafraîchit le token en utilisant un refresh token valide.
    ///<param name="refreshToken"></param>
    /// </summary>
    [HttpPut("refresh-token")]
    [AllowAnonymous]
    public async Task<ActionResult> RegenerateAccessTokenUsingRefreshToken([FromBody] string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return BadRequest();
        email = HttpContext.Session.GetString("email");
        password = HttpContext.Session.GetString("password");
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return BadRequest("Email or password is missing. Could not refresh Token");
        var result = await jwtToken.NewAccessTokenUsingRefreshTokenInRedisAsync(refreshToken, email, password);
        if (!result.Response)
            return Unauthorized(new { result.Message });
        return CreatedAtAction(nameof(RegenerateAccessTokenUsingRefreshToken), new { result.Token, result.RefreshToken });
    }
}