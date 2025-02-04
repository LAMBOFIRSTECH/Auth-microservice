using Xunit;
using Authentifications.Models;
using System.ComponentModel.DataAnnotations;
namespace Authentifications.Tests;
public class UnitTestUtilisateurDto
{
    [Fact]
    public void ID_ShouldBeGeneratedAutomatically()
    {
        // Arrange
        var utilisateur = new UtilisateurDto();

        // Act
        var id = utilisateur.ID;

        // Assert
        Assert.Equal(Guid.Empty, id);
    }
    [Fact]
    public void Nom_ShouldNotExceedMaxLength()
    {
        // Arrange
        var utilisateur = new UtilisateurDto();
        var longName = new string('a', 21);

        // Act
        utilisateur.Nom = longName;
        var context = new ValidationContext(utilisateur, null, null);
        var results = new List<ValidationResult>();

        // Assert
        var isValid = Validator.TryValidateObject(utilisateur, context, results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == "Username cannot exceed 20 characters");
    }
    [Fact]
    public void Email_ShouldBeRequired()
    {
        // Arrange
        var utilisateur = new UtilisateurDto();

        // Act
        var context = new ValidationContext(utilisateur, null, null);
        var results = new List<ValidationResult>();

        // Assert
        var isValid = Validator.TryValidateObject(utilisateur, context, results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage.Contains("The Email field is required"));
    }
    [Fact]
    public void Email_ShouldBeValidFormat()
    {
        // Arrange
        var utilisateur = new UtilisateurDto { Email = "invalid-email" };

        // Act
        var context = new ValidationContext(utilisateur, null, null);
        var results = new List<ValidationResult>();

        // Assert
        var isValid = Validator.TryValidateObject(utilisateur, context, results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage.Contains("The Email field is not a valid e-mail address"));
    }
    [Fact]
    public void Pass_ShouldBeRequired()
    {
        // Arrange
        var utilisateur = new UtilisateurDto();

        // Act
        var context = new ValidationContext(utilisateur, null, null);
        var results = new List<ValidationResult>();

        // Assert
        var isValid = Validator.TryValidateObject(utilisateur, context, results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage.Contains("The Pass field is required"));
    }
    [Fact]
    public void CheckHashPassword_ShouldReturnTrueForValidPassword()
    {
        // Arrange
        var utilisateur = new UtilisateurDto { Pass = BCrypt.Net.BCrypt.HashPassword("password123") };

        // Act
        var isValid = utilisateur.CheckHashPassword("password123");

        // Assert
        Assert.True(isValid);
    }
    [Fact]
    public void CheckHashPassword_ShouldReturnFalseForInvalidPassword()
    {
        // Arrange
        var utilisateur = new UtilisateurDto { Pass = BCrypt.Net.BCrypt.HashPassword("password123") };

        // Act
        var isValid = utilisateur.CheckHashPassword("wrongpassword");

        // Assert
        Assert.False(isValid);
    }
    [Fact]
    public void Role_ShouldBeSetToAdministrateur()
    {
        // Arrange
        var utilisateur = new UtilisateurDto
        {
            // Act
            Role = UtilisateurDto.Privilege.Administrateur
        };

        // Assert
        Assert.Equal(UtilisateurDto.Privilege.Administrateur, utilisateur.Role);
    }

    [Fact]
    public void Role_ShouldBeSetToUtilisateur()
    {
        // Arrange
        var utilisateur = new UtilisateurDto
        {
            // Act
            Role = UtilisateurDto.Privilege.Utilisateur
        };

        // Assert
        Assert.Equal(UtilisateurDto.Privilege.Utilisateur, utilisateur.Role);
    }
    [Fact]
    public void Email_WithValidEmail_ShouldPassValidation()
    {
        // Arrange
        var utilisateur = new UtilisateurDto { Email = "test@example.com" };
        var validationContext = new ValidationContext(utilisateur) { MemberName = "Email" };
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateProperty(utilisateur.Email, validationContext, validationResults);

        // Assert
        Assert.True(isValid);
    }
    [Fact]
    public void Email_WithInvalidEmail_ShouldFailValidation()
    {
        // Arrange
        var utilisateur = new UtilisateurDto { Email = "invalid-email" };
        var validationContext = new ValidationContext(utilisateur) { MemberName = "Email" };
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateProperty(utilisateur.Email, validationContext, validationResults);

        // Assert
        Assert.False(isValid);
        Assert.Single(validationResults);
        Assert.Equal("The Email field is not a valid e-mail address.", validationResults[0].ErrorMessage);
    }
    [Fact]
    public void CheckHashPassword_ValidPassword_ReturnsTrue()
    {
        // Arrange
        var utilisateur = new UtilisateurDto();
        const string password = "securePassword123";
        utilisateur.Pass = BCrypt.Net.BCrypt.HashPassword(password);

        // Act
        bool result = utilisateur.CheckHashPassword(password);

        // Assert
        Assert.True(result);
    }
    [Fact]
    public void CheckHashPassword_InvalidPassword_ReturnsFalse()
    {
        // Arrange
        var utilisateur = new UtilisateurDto();
        const string password = "securePassword123";
        utilisateur.Pass = BCrypt.Net.BCrypt.HashPassword(password);
        const string wrongPassword = "wrongPassword";

        // Act
        bool result = utilisateur.CheckHashPassword(wrongPassword);

        // Assert
        Assert.False(result);
    }
}