using System.ComponentModel.DataAnnotations;
namespace Authentifications.Application.DTOs;
/// <summary>
/// Gestion de la reponse du token JWT.
/// </summary>
public class TokenResultDto
{
    public bool Response { get; set; }
    [MaxLength(50, ErrorMessage = "Message cannot exceed 50 characters")]
    public string? Message { get; set; }
    [Required]
    public string? Token { get; set; }
    [Required]
    public string? RefreshToken { get; set; }
}