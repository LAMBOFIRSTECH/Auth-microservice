namespace Authentifications.Core.Entities;
public class Message
{
    public string? Type { get; set; }
    public string? Title { get; set; }
    public string? Detail { get; set; }
    public int Status { get; set; }
    public string? TraceId { get; set; }
}
