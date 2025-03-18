using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Security.Cryptography;
namespace Authentifications.Application.DTOs;
public class UtilisateurDto
{
    /// <summary>
    /// Représente l'identifiant unique d'un utilisateur.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid ID { get; set; }
    [MaxLength(20, ErrorMessage = "Username cannot exceed 20 characters")]
    public string Nom { get; set; } = string.Empty;
    [Required]
    [EmailAddress]
    public string? Email { get; set; }
    public enum Privilege { Administrateur, Utilisateur }
    [EnumDataType(typeof(Privilege))]
    [Required]
    public Privilege Role { get; set; }
    [Required]
    [Category("Security")]
    public string? Pass { get; set; }
    public bool CheckHashPassword(string password, string storedHashBase64)
    {
        if (!storedHashBase64.StartsWith("{SHA512}"))
            return false;
        string base64Hash = storedHashBase64.Replace("{SHA512}", "").Trim();

        // Convertir le hash stocké (Base64 → byte[])
        byte[] storedHashBytes = Convert.FromBase64String(base64Hash);
        using SHA512 sha512 = SHA512.Create();
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        byte[] computedHash = sha512.ComputeHash(passwordBytes);
        return CryptographicOperations.FixedTimeEquals(computedHash, storedHashBytes);
    }
}