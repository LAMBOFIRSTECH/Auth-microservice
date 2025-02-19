using Xunit;
using Authentifications.Models;
using System.ComponentModel.DataAnnotations;
namespace Authentifications.Tests;
public class UnitTestTokenResultModel
{
    [Fact]
    public void Response_ShouldBeSetAndRetrievedCorrectly()
    {
        var tokenResult = new TokenResult
        {
            Response = true
        };
        Assert.True(tokenResult.Response);
    }
    [Fact]
    public void Message_ShouldNotExceedMaxLength()
    {
        var tokenResult = new TokenResult();
        var context = new ValidationContext(tokenResult);
        var results = new List<ValidationResult>();
        tokenResult.Message = new string('a', 51);
        var isValid = Validator.TryValidateObject(tokenResult, context, results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == "Username cannot exceed 50 characters");
    }
    [Fact]
    public void Token_ShouldBeRequired()
    {
        var tokenResult = new TokenResult();
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
        var tokenResult = new TokenResult();
        var context = new ValidationContext(tokenResult);
        var results = new List<ValidationResult>();
        tokenResult.RefreshToken = null;
        var isValid = Validator.TryValidateObject(tokenResult, context, results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.MemberNames.Contains("RefreshToken"));
    }
}
