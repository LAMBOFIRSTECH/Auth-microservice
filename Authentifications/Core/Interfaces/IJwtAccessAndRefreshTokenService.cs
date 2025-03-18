using Authentifications.Application.DTOs;
namespace Authentifications.Core.Interfaces;
public interface IJwtAccessAndRefreshTokenService
{
	abstract string GenerateRefreshToken();
	TokenResultDto GetToken(UtilisateurDto utilisateurDto);
	Task<UtilisateurDto> AuthUserDetailsAsync((bool IsValid, string email, string password) tupleParameter);
	Task<TokenResultDto>  NewAccessTokenUsingRefreshTokenInRedisAsync(string refreshToken, string email, string password);
}
