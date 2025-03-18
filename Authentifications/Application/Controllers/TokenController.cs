using Microsoft.AspNetCore.Mvc;
using Authentifications.Core.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Authentifications.Application.DTOs;
namespace Authentifications.Application.Controllers;
[Route("auth")]
public class TokenController : ControllerBase
{
    private readonly IJwtAccessAndRefreshTokenService jwtToken;
    public TokenController(IJwtAccessAndRefreshTokenService jwtToken)
    {
        this.jwtToken = jwtToken;
    }
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
            return Unauthorized("Unauthorized access 💢");
        var user = await jwtToken.AuthUserDetailsAsync((User.Identity!.IsAuthenticated, email, password));
        var tokenResult = jwtToken.GetToken(user);
        if (!tokenResult.Response)
        {
            return Unauthorized(new TokenResultDto
            {
                Response = tokenResult.Response,
                Message = tokenResult.Message,
                Token = tokenResult.Token,
                RefreshToken = tokenResult.RefreshToken
            });
        }
        HttpContext.Session.SetString("email", email);
        HttpContext.Session.SetString("password", password);
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
        var tokenResult = await jwtToken.NewAccessTokenUsingRefreshTokenInRedisAsync(refreshToken, email, password);
        if (!tokenResult.Response)
        {
            return Unauthorized(new TokenResultDto
            {
                Response = tokenResult.Response,
                Message = tokenResult.Message,
                Token = tokenResult.Token,
                RefreshToken = tokenResult.RefreshToken
            });
        }
        return CreatedAtAction(nameof(RegenerateAccessTokenUsingRefreshToken), new { tokenResult });
    }
}