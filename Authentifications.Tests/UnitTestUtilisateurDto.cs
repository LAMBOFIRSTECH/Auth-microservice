using Moq;
using Xunit;
using Authentifications.Models;
namespace Authentifications.Tests;
public class UnitTestUtilisateurDto
{
    [Fact]
    public void Role_ShouldBeSetToAdministrateur()
    {
        // Arrange
        var utilisateur = new UtilisateurDto();

        // Act
        utilisateur.Role = UtilisateurDto.Privilege.Administrateur;

        // Assert
        Assert.Equal(UtilisateurDto.Privilege.Administrateur, utilisateur.Role);
    }

    [Fact]
    public void Role_ShouldBeSetToUtilisateur()
    {
        // Arrange
        var utilisateur = new UtilisateurDto();

        // Act
        utilisateur.Role = UtilisateurDto.Privilege.Utilisateur;

        // Assert
        Assert.Equal(UtilisateurDto.Privilege.Utilisateur, utilisateur.Role);
    }
}