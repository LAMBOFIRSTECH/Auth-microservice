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
    public TokenController(IJwtAccessAndRefreshTokenService jwtToken)
    { this.jwtToken = jwtToken; }
    /// <summary>
    /// Authentifie un utilisateur et retourne les tokens (access et refresh).
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult> Authentificate()
    {
        var email = HttpContext.Items["email"] as string;
        var password = HttpContext.Items["password"] as string;
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return BadRequest("Email or password is missing.");
        if (!User.Identity!.IsAuthenticated)
            return Unauthorized("Unauthorized access");
        var user = await jwtToken.AuthUserDetailsAsync((User.Identity!.IsAuthenticated, email, password));
        var result = jwtToken.GetToken(user);
        if (!result.Response)
        {
            return Unauthorized(new TokenResult
            {
                Response = result.Response,
                Message = result.Message,
                Token = null,
                RefreshToken = null
            });
        }

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
        var email = HttpContext.Session.GetString("email");
        var password = HttpContext.Session.GetString("password");
        if (string.IsNullOrWhiteSpace(refreshToken))
            return BadRequest(new { Message = "Refresh token is required." });
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return BadRequest(new { Message = "Email or password is missing. Could not refresh token." });
        var result = await jwtToken.NewAccessTokenUsingRefreshTokenInRedisAsync(refreshToken, email, password);
        if (!result.Response)
            return Unauthorized(new { result.Message });
        return CreatedAtAction(nameof(RegenerateAccessTokenUsingRefreshToken), new { result.Token, result.RefreshToken });
    }
}