using System.ComponentModel.DataAnnotations;

namespace Authentifications.Models;
/// <summary>
/// Gestion de la reponse du token JWT.
/// </summary>
public class TokenResult
{
    public bool Response { get; set; }
    [MaxLength(50, ErrorMessage = "Username cannot exceed 50 characters")]
    public string? Message { get; set; }
    [Required]
    public string? Token { get; set; }
    [Required]
    public string? RefreshToken { get; set; }
}
