using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.Access;
using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.WebPortal.Managers;
public class MassMailManager(
    IClaimsRepository claimsRepository,
    IEmailService emailService,
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository userRepository)
{
    public async Task MassMail(
        ProjectIdentification projectId,
        int[] claimIds,
        MarkdownString body,
        string subject,
        bool alsoMailToMasters)
    {
        var claims = (await claimsRepository.GetClaimsByIds(projectId, claimIds)).ToArray();
        if (claims.Length == 0)
        {
            return;
        }
        var project = claims.Select(c => c.Project).First();
        _ = project.EnsureProjectActive();
        if (!project.HasMasterAccess(currentUserAccessor, Permission.CanSendMassMails))
        {
            claims = claims.Where(claim => claim.ResponsibleMasterUserId == currentUserAccessor.UserId).ToArray();
        }

        var recipients =
            claims
                .Select(c => c.Player)
                .UnionIf(project.ProjectAcls.Select(acl => acl.User), alsoMailToMasters);

        await emailService.Email(new MassEmailModel()
        {
            Initiator = await userRepository.GetById(currentUserAccessor.UserIdentification),
            ProjectName = project.ProjectName,
            Text = body,
            Recipients = recipients.ToList(),
            Subject = subject,
        });
    }
}
