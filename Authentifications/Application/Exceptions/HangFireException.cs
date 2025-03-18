namespace Authentifications.Application.Exceptions;
public class HangFireException : Exception
{
    public HangFireException(int Status, string Type,string message) : base(message) { }
}