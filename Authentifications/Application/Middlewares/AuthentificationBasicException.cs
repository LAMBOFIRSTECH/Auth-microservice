namespace Authentifications.Application.Middlewares;
public class AuthentificationBasicException :Exception
{
	public AuthentificationBasicException(string message) : base(message) { }

    public AuthentificationBasicException() : base()
    {
    }

    public AuthentificationBasicException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}