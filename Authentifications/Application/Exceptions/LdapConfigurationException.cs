namespace Authentifications.Application.Exceptions;
public class LdapConfigurationException : Exception
{
    public LdapConfigurationException(int Status, string Type,string message) : base(message) { }
}