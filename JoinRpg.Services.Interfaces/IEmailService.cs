using System.Collections.Generic;
using System.Threading.Tasks;
using JoinRpg.DataModel;

namespace JoinRpg.Services.Interfaces
{
  public interface IEmailService
  {
    Task SendEmail(IEnumerable<User> users, string templateName, dynamic viewBag);
  }
}
