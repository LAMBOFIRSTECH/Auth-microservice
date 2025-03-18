using Authentifications.Core.Entities;
namespace Authentifications.Core.Interfaces;
public interface ILdapService { Task<Message> RetrieveLdapData(string message); }