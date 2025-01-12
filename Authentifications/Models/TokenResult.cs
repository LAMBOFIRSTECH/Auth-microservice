namespace Authentifications.Models
{
    /// <summary>
    /// Gestion de la reponse du token JWT.
    /// </summary>

    public class TokenResult
	{
		public bool Response { get; set; }
		public string? Message { get; set; }
		public string? Token { get; set; }
		public string? RefreshToken { get; set; }
	}
}