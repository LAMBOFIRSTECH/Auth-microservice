namespace Authentifications.Application.Exceptions;
public class VaultConfigurationException : Exception
{
    public VaultConfigurationException(int Status, string Type,string message) : base(message) { }
}