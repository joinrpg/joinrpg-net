using JoinRpg.Data.Write.Interfaces;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl
{
  public abstract class ClaimImplBase : DbServiceImplBase
  {
    protected IEmailService EmailService { get; }

    protected ClaimImplBase(IUnitOfWork unitOfWork, IEmailService emailService) : base(unitOfWork)
    {
      EmailService = emailService;
    }
  }
}
