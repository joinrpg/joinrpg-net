using JoinRpg.Data.Write.Interfaces;
using JoinRpg.Domain.CharacterFields;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl
{
  public abstract class ClaimImplBase : DbServiceImplBase
  {
    protected IEmailService EmailService { get; }
    protected IFieldDefaultValueGenerator FieldDefaultValueGenerator { get; }

    protected ClaimImplBase(IUnitOfWork unitOfWork, IEmailService emailService,
      IFieldDefaultValueGenerator fieldDefaultValueGenerator) : base(unitOfWork)
    {
      EmailService = emailService;
      FieldDefaultValueGenerator = fieldDefaultValueGenerator;
    }
  }
}
