using Xunit;
using Authentifications.Application.DTOs;
using System.ComponentModel.DataAnnotations;
namespace Authentifications.Tests;
public class UnitTestTokenResultEntity
{
    [Fact]
    public void Response_ShouldBeSetAndRetrievedCorrectly()
    {
        var tokenResult = new TokenResultDto
        {
            Response = true
        };
        Assert.True(tokenResult.Response);
    }
    [Fact]
    public void Message_ShouldNotExceedMaxLength()
    {
        var tokenResult = new TokenResultDto();
        var longMessage = new string('a', 51); // Exceeding the max length of 50
        tokenResult.Message = longMessage;
        var context = new ValidationContext(tokenResult);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(tokenResult, context, results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == "Message cannot exceed 50 characters");
    }
    [Fact]
    public void Token_ShouldBeRequired()
    {
        var tokenResult = new TokenResultDto();
        var context = new ValidationContext(tokenResult);
        var results = new List<ValidationResult>();
        tokenResult.Token = null;
        var isValid = Validator.TryValidateObject(tokenResult, context, results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.MemberNames.Contains("Token"));
    }
    [Fact]
    public void RefreshToken_ShouldBeRequired()
    {
        var tokenResult = new TokenResultDto();
        var context = new ValidationContext(tokenResult);
        var results = new List<ValidationResult>();
        tokenResult.RefreshToken = null;
        var isValid = Validator.TryValidateObject(tokenResult, context, results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.MemberNames.Contains("RefreshToken"));
    }
}