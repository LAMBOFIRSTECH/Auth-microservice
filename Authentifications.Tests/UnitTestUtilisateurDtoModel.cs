using Xunit;
using Authentifications.Models;
using System.ComponentModel.DataAnnotations;
namespace Authentifications.Tests;
public class UnitTestUtilisateurDtoModel
{
    [Fact]
    public void ID_ShouldBeGeneratedAutomatically()
    {
        var utilisateur = new UtilisateurDto();
        var id = utilisateur.ID;
        Assert.Equal(Guid.Empty, id);
    }
    [Fact]
    public void Nom_ShouldNotExceedMaxLength()
    {
        var utilisateur = new UtilisateurDto();
        var longName = new string('a', 21);
        utilisateur.Nom = longName;
        var context = new ValidationContext(utilisateur, null, null);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(utilisateur, context, results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == "Username cannot exceed 20 characters");
    }
    [Fact]
    public void Email_ShouldBeRequired()
    {
        var utilisateur = new UtilisateurDto();
        var context = new ValidationContext(utilisateur, null, null);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(utilisateur, context, results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage.Contains("The Email field is required"));
    }
    [Fact]
    public void Email_ShouldBeValidFormat()
    {
        var utilisateur = new UtilisateurDto { Email = "invalid-email" };
        var context = new ValidationContext(utilisateur, null, null);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(utilisateur, context, results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage.Contains("The Email field is not a valid e-mail address"));
    }
    [Fact]
    public void Pass_ShouldBeRequired()
    {
        var utilisateur = new UtilisateurDto();
        var context = new ValidationContext(utilisateur, null, null);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(utilisateur, context, results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage.Contains("The Pass field is required"));
    }
    [Fact]
    public void CheckHashPassword_ShouldReturnTrueForValidPassword()
    {
        var utilisateur = new UtilisateurDto { Pass = BCrypt.Net.BCrypt.HashPassword("password123") };
        var isValid = utilisateur.CheckHashPassword("password123");
        Assert.True(isValid);
    }
    [Fact]
    public void CheckHashPassword_ShouldReturnFalseForInvalidPassword()
    {
        var utilisateur = new UtilisateurDto { Pass = BCrypt.Net.BCrypt.HashPassword("password123") };
        var isValid = utilisateur.CheckHashPassword("wrongpassword");
        Assert.False(isValid);
    }
    [Fact]
    public void Role_ShouldBeSetToAdministrateur()
    {
        var utilisateur = new UtilisateurDto
        {
            Role = UtilisateurDto.Privilege.Administrateur
        };
        Assert.Equal(UtilisateurDto.Privilege.Administrateur, utilisateur.Role);
    }

    [Fact]
    public void Role_ShouldBeSetToUtilisateur()
    {
        var utilisateur = new UtilisateurDto
        {
            Role = UtilisateurDto.Privilege.Utilisateur
        };
        Assert.Equal(UtilisateurDto.Privilege.Utilisateur, utilisateur.Role);
    }
    [Fact]
    public void Email_WithValidEmail_ShouldPassValidation()
    {
        var utilisateur = new UtilisateurDto { Email = "test@example.com" };
        var validationContext = new ValidationContext(utilisateur) { MemberName = "Email" };
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateProperty(utilisateur.Email, validationContext, validationResults);
        Assert.True(isValid);
    }
    [Fact]
    public void Email_WithInvalidEmail_ShouldFailValidation()
    {
        var utilisateur = new UtilisateurDto { Email = "invalid-email" };
        var validationContext = new ValidationContext(utilisateur) { MemberName = "Email" };
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateProperty(utilisateur.Email, validationContext, validationResults);
        Assert.False(isValid);
        Assert.Single(validationResults);
        Assert.Equal("The Email field is not a valid e-mail address.", validationResults[0].ErrorMessage);
    }
    [Fact]
    public void CheckHashPassword_ValidPassword_ReturnsTrue()
    {
        var utilisateur = new UtilisateurDto();
        const string password = "securePassword123";
        utilisateur.Pass = BCrypt.Net.BCrypt.HashPassword(password);
        bool result = utilisateur.CheckHashPassword(password);
        Assert.True(result);
    }
    [Fact]
    public void CheckHashPassword_InvalidPassword_ReturnsFalse()
    {
        var utilisateur = new UtilisateurDto();
        const string password = "securePassword123";
        utilisateur.Pass = BCrypt.Net.BCrypt.HashPassword(password);
        const string wrongPassword = "wrongPassword";
        bool result = utilisateur.CheckHashPassword(wrongPassword);
        Assert.False(result);
    }
}
