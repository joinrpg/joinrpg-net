using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.CharacterFields;
using JoinRpg.Domain.Problems;
using JoinRpg.PrimitiveTypes.Access;
using JoinRpg.PrimitiveTypes.Claims;
using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.Services.Impl;

internal class ClaimServiceImpl(
    IUnitOfWork unitOfWork,
    IEmailService emailService,
    FieldSaveHelper fieldSaveHelper,
    IAccommodationInviteService accommodationInviteService,
    ICurrentUserAccessor currentUserAccessor,
    IProjectMetadataRepository projectMetadataRepository,
    IProblemValidator<Claim> claimValidator,
    ILogger<CharacterServiceImpl> logger,
    IClaimNotificationService claimNotificationService,
    CommentHelper commentHelper
    )
    : ClaimImplBase(unitOfWork, emailService, currentUserAccessor, projectMetadataRepository, commentHelper), IClaimService
{
    // TODO в GameSubscribeService
    public async Task SubscribeClaimToUser(int projectId, int claimId)
    {
        var user = await UserRepository.GetWithSubscribe(CurrentUserId);
        _ = (await ClaimsRepository.GetClaim(projectId, claimId)).RequestAccess(CurrentUserId);
        _ = user.Subscriptions.Add(
            new UserSubscription() { ClaimId = claimId, ProjectId = projectId }.AssignFrom(
                SubscriptionOptions.CreateAllSet()));
        await UnitOfWork.SaveChangesAsync();
    }

    // TODO в GameSubscribeService
    public async Task UnsubscribeClaimToUser(int projectId, int claimId)
    {
        var user = await UserRepository.GetWithSubscribe(CurrentUserId);
        _ = (await ClaimsRepository.GetClaim(projectId, claimId)).RequestAccess(CurrentUserId);
        var subscription = user.Subscriptions.FirstOrDefault(s =>
            s.ProjectId == projectId && s.UserId == CurrentUserId && s.ClaimId == claimId);
        if (subscription != null)
        {
            _ = UnitOfWork.GetDbSet<UserSubscription>().Remove(subscription);
            await UnitOfWork.SaveChangesAsync();
        }
    }

    public async Task CheckInClaim(ClaimIdentification claimId, int money)
    {
        var (claim, projectInfo) = await LoadClaimAsMaster(claimId); //TODO Specific right
        claim.EnsureCanChangeStatus(ClaimStatus.CheckedIn);

        var validator = new ClaimCheckInValidator(claim, claimValidator, projectInfo);
        if (!validator.CanCheckInInPrinciple)
        {
            throw new ClaimWrongStatusException(claim);
        }

        ClaimSimpleChangedNotification? financeEmail = null;
        if (money > 0)
        {
            var paymentType = projectInfo.ProjectFinanceSettings.GetCashPaymentType(currentUserAccessor.UserIdentification) ?? throw new JoinRpgInvalidUserException();

            financeEmail = AcceptFeeImpl(".", Now, money, paymentType, claim, projectInfo);
        }
        else if (money < 0)
        {
            throw new InvalidOperationException();
        }

        if (!validator.CanCheckInNow)
        {
            throw new ClaimWrongStatusException(claim);
        }

        claim.ClaimStatus = ClaimStatus.CheckedIn;
        claim.CheckInDate = Now;
        MarkChanged(claim.Character);
        claim.Character.InGame = true;

        var (_, email) = CommentHelper.AddClaimCommentWithNotification("", claim, projectInfo, CommentExtraAction.CheckedIn, ClaimOperationType.MasterVisibleChange, Now);

        await UnitOfWork.SaveChangesAsync();

        await claimNotificationService.SendNotification(email);

        if (financeEmail != null)
        {
            await claimNotificationService.SendNotification(financeEmail);
        }
    }

    public async Task<int> MoveToSecondRole(ClaimIdentification claimId, CharacterIdentification characterId, string secondRoleCommentText)
    {
        if (claimId.ProjectId != characterId.ProjectId)
        {
            throw new InvalidOperationException("Нельзя смешивать разные проекты в запросе");
        }
        var (oldClaim, projectInfo) = await LoadClaimAsMaster(claimId); //TODO Specific right
        oldClaim.EnsureStatus(ClaimStatus.CheckedIn);

        oldClaim.Character.InGame = false;
        MarkChanged(oldClaim.Character);

        var source = await CharactersRepository.GetCharacterAsync(characterId);

        MarkChanged(source);

        // TODO improve valitdation here
        //source.EnsureCanMoveClaim(oldClaim);

        var responsibleMaster = source.GetResponsibleMaster();
        var claim = new Claim()
        {
            CharacterId = characterId.CharacterId,
            ProjectId = characterId.ProjectId,
            PlayerUserId = oldClaim.PlayerUserId,
            PlayerAcceptedDate = Now,
            CreateDate = Now,
            ClaimStatus = ClaimStatus.Approved,
            CurrentFee = 0,
            ResponsibleMasterUserId = responsibleMaster.UserId,
            ResponsibleMasterUser = responsibleMaster,
            LastUpdateDateTime = Now,
            MasterAcceptedDate = Now,
            CommentDiscussion =
            new CommentDiscussion() { CommentDiscussionId = -1, ProjectId = characterId.ProjectId },
        };

        claim.CommentDiscussion.Comments.Add(new Comment
        {
            CommentDiscussionId = -1,
            AuthorUserId = CurrentUserId,
            CommentText = new CommentText { Text = new MarkdownString(secondRoleCommentText) },
            CreatedAt = Now,
            IsCommentByPlayer = false,
            IsVisibleToPlayer = true,
            ProjectId = characterId.ProjectId,
            LastEditTime = Now,
            ExtraAction = CommentExtraAction.SecondRole,
        });

        oldClaim.ClaimStatus = ClaimStatus.Approved;
        source.ApprovedClaim = claim;

        var (_, email) = CommentHelper.AddClaimCommentWithNotification(secondRoleCommentText, oldClaim, projectInfo, CommentExtraAction.OutOfGame, ClaimOperationType.MasterVisibleChange, Now);

        email = email with { AnotherCharacterId = characterId };


        _ = UnitOfWork.GetDbSet<Claim>().Add(claim);

        _ = fieldSaveHelper.SaveCharacterFields(CurrentUserId, claim, new Dictionary<int, string?>(), projectInfo);

        await UnitOfWork.SaveChangesAsync();

        await claimNotificationService.SendNotification(email);

        return claim.ClaimId;
    }

    public async Task<ClaimIdentification> AddClaimFromUser(CharacterIdentification characterId,
        string claimText,
        IReadOnlyDictionary<int, string?> fields, bool sensitiveDataAllowed)
    {

        logger.LogDebug("About to add claim to character {characterId}", characterId);

        var source = await CharactersRepository.GetCharacterAsync(characterId);
        var projectInfo = await ProjectMetadataRepository.GetProjectMetadata(characterId.ProjectId);
        var user = await UserRepository.GetRequiredUserInfo(currentUserAccessor.UserIdentification);

        source.EnsureCanAddClaim(user, projectInfo);

        User responsibleMaster = source.GetResponsibleMaster();

        var claim = new Claim()
        {
            CharacterId = characterId.CharacterId,
            ProjectId = characterId.ProjectId,
            PlayerUserId = CurrentUserId,
            PlayerAcceptedDate = Now,
            CreateDate = Now,
            ClaimStatus = ClaimStatus.AddedByUser,
            ResponsibleMasterUserId = responsibleMaster.UserId,
            ResponsibleMasterUser = responsibleMaster,
            LastUpdateDateTime = Now,
            PlayerAllowedSenstiveData = sensitiveDataAllowed && projectInfo.ProfileRequirementSettings.SensitiveDataRequired,
            CommentDiscussion = new CommentDiscussion() { CommentDiscussionId = -1, ProjectId = characterId.ProjectId },
        };

        if (!string.IsNullOrWhiteSpace(claimText))
        {
            var comment = CommentHelper.CreateCommentForClaim(claim,
                Now,
                claimText,
                ClaimOperationType.PlayerChange,
                projectInfo,
                CommentExtraAction.NewClaim);
        }

        _ = UnitOfWork.GetDbSet<Claim>().Add(claim);

        var updatedFields = fieldSaveHelper.SaveCharacterFields(CurrentUserId, claim, fields, projectInfo);

        var claimEmail = await CreateClaimEmail<NewClaimEmail>(claim, claimText ?? "", s => s.ClaimStatusChange,
          CommentExtraAction.NewClaim);

        claimEmail.UpdatedFields = updatedFields;

        await UnitOfWork.SaveChangesAsync();

        await EmailService.Email(claimEmail);

        var claimId = claim.GetId();

        if (claim.Project.Details.AutoAcceptClaims &&
            (claim.PlayerAllowedSenstiveData || !projectInfo.ProfileRequirementSettings.SensitiveDataRequired))
        // Не принимаем автоматически заявки, если игрок не предоставил доступ к паспорту
        {
            StartImpersonate(claim.ResponsibleMasterUserId);
            //TODO[Localize]
            await ApproveByMaster(claimId, "Ваша заявка была принята автоматически");
            ResetImpersonation();
        }

        logger.LogInformation("Claim ({claimId}) was successfully send", claimId);
        return claimId;
    }

    public async Task AddComment(ClaimIdentification claimId, int? parentCommentId, bool isVisibleToPlayer, string commentText, FinanceOperationAction financeAction)
    {
        var (claim, projectInfo) = await LoadClaimAsMaster(claimId, Permission.None, ExtraAccessReason.Player);

        ClaimOperationType claimOperationType;

        if (claim.PlayerUserId == CurrentUserId)
        {
            claimOperationType = ClaimOperationType.PlayerChange;
        }
        else
        {
            claimOperationType = isVisibleToPlayer ? ClaimOperationType.MasterVisibleChange : ClaimOperationType.MasterSecretChange;
        }

        SetDiscussed(claim, isVisibleToPlayer);

        var parentComment = claim.CommentDiscussion.Comments.SingleOrDefault(c => c.CommentId == parentCommentId);

        CommentExtraAction? extraAction = null;

        if (financeAction != FinanceOperationAction.None)
        {
            if (parentComment is null)
            {
                throw new InvalidOperationException("Requested to perform finance operation on parent comment, but there is no any");
            }
            extraAction = PerformFinanceOperation(financeAction, parentComment, claim, projectInfo);
        }


        var result = CommentHelper.AddClaimCommentWithNotification(commentText, claim, projectInfo, extraAction, claimOperationType, Now);

        if (parentComment is not null)
        {
            SetParentCommentAndCheck(result, parentComment, claimOperationType);
        }

        await UnitOfWork.SaveChangesAsync();

        await claimNotificationService.SendNotification(result.Item2);
    }

    private CommentExtraAction? PerformFinanceOperation(FinanceOperationAction financeAction,
      Comment parentComment, Claim claim, ProjectInfo projectInfo)
    {
        var finance = parentComment?.Finance;
        if (finance == null)
        {
            throw new InvalidOperationException();
        }

        if (!finance.RequireModeration)
        {
            throw new ValueAlreadySetException("Finance entry is already moderated.");
        }

        finance.RequestModerationAccess(CurrentUserId);
        finance.Changed = Now;
        switch (financeAction)
        {
            case FinanceOperationAction.Approve:
                finance.State = FinanceOperationState.Approved;
                if (finance.OperationType == FinanceOperationType.PreferentialFeeRequest)
                {
                    claim.PreferentialFeeUser = true;
                }
                claim.UpdateClaimFeeIfRequired(finance.OperationDate, projectInfo);
                return CommentExtraAction.ApproveFinance;
            case FinanceOperationAction.Decline:
                finance.State = FinanceOperationState.Declined;
                return CommentExtraAction.RejectFinance;
            case FinanceOperationAction.None:
            default:
                throw new ArgumentOutOfRangeException(nameof(financeAction), financeAction, null);
        }
    }

    public async Task ApproveByMaster(ClaimIdentification claimId, string commentText)
    {
        var (claim, projectInfo) = await LoadClaimForApprovalDecline(claimId);

        if (claim.ClaimStatus == ClaimStatus.CheckedIn)
        {
            throw new ClaimWrongStatusException(claim);
        }

        commentText ??= "";

        if (claim.Character.CharacterType == CharacterType.Slot)
        {
            var character = await CreateCharacterFromSlot(claim.Character, claim.Player);
            claim.Character = character;
            claim.CharacterId = character.CharacterId;
        }

        claim.MasterAcceptedDate = Now;
        claim.ChangeStatusWithCheck(ClaimStatus.Approved);

        var (_, email) = CommentHelper.AddClaimCommentWithNotification(commentText, claim, projectInfo, CommentExtraAction.ApproveByMaster, ClaimOperationType.MasterVisibleChange, Now);

        List<ClaimSimpleChangedNotification> notificationsList = [email];

        if (!claim.Project.Details.EnableManyCharacters)
        {
            foreach (var otherClaim in claim.OtherPendingClaimsForThisPlayer())
            {
                otherClaim.EnsureCanChangeStatus(ClaimStatus.DeclinedByMaster);
                otherClaim.MasterDeclinedDate = Now;
                otherClaim.ClaimStatus = ClaimStatus.DeclinedByMaster;

                var (_, otherEmail) = CommentHelper.AddClaimCommentWithNotification(
                    "Заявка автоматически отклонена, т.к. другая заявка того же игрока была принята в тот же проект",
                    otherClaim,
                    projectInfo,
                    CommentExtraAction.DeclineByMaster,
                    ClaimOperationType.MasterVisibleChange, Now);

                notificationsList.Add(otherEmail);
            }
        }

        MarkCharacterChangedIfApproved(claim);
        claim.Character.ApprovedClaimId = claim.ClaimId;
        claim.Character.ApprovedClaim = claim; // Used in SaveCharacterFields
        claim.Character.IsHot = false;

        //We need to re-save fields here. Reasons:
        // 1. If we created character during approving, we need to set name for character
        // 2. M.b. we need to move some field values from Claim to Characters
        // 3. (2) Could activate changing of special groups
        // we don't need send to show updated fields in email here, so ignore return result. 
        _ = fieldSaveHelper.SaveCharacterFields(CurrentUserId, claim, new Dictionary<int, string?>(), projectInfo);

        await UnitOfWork.SaveChangesAsync();

        foreach (var notification in notificationsList)
        {
            await claimNotificationService.SendNotification(notification);
        }
    }

    private async Task<Character> CreateCharacterFromSlot(Character slot, User player)
    {

        switch (slot.CharacterSlotLimit)
        {
            case null:  // Unlimited slot
                break;
            case > 0:
                slot.CharacterSlotLimit--;
                break;
            default:
                throw new JoinRpgSlotLimitedException(slot);
        }


        if (slot.CharacterType != CharacterType.Slot)
        {
            throw new EntityWrongStatusException(slot);
        }

        var newCharacter = new Character()
        {
            ApprovedClaim = null,
            ApprovedClaimId = null,
            AutoCreated = true,
            OriginalCharacterSlot = slot,
            CanBePermanentlyDeleted = false,
            CharacterId = -1,
            CharacterName = slot.CharacterName, // Probably will be updated by field save
            CharacterSlotLimit = null,
            CharacterType = CharacterType.Player,
            CreatedAt = DateTime.Now,
            CreatedBy = player,
            CreatedById = player.UserId,
            DirectlyRelatedPlotElements = slot.DirectlyRelatedPlotElements,
            HidePlayerForCharacter = slot.HidePlayerForCharacter,
            InGame = false,
            IsAcceptingClaims = true,
            IsActive = true,
            IsHot = false,
            IsPublic = slot.IsPublic,
            JsonData = slot.JsonData,
            ParentCharacterGroupIds = slot.ParentCharacterGroupIds,
            PlotElementOrderData = slot.PlotElementOrderData,
            Project = slot.Project,
            ProjectId = slot.ProjectId,
            Subscriptions = slot.Subscriptions,
            UpdatedAt = DateTime.Now,
            UpdatedBy = player,
            UpdatedById = player.UserId,
        };

        var plots = await PlotRepository.GetPlotsForCharacter(slot);

        foreach (var plot in plots)
        {
            if (plot.TargetCharacters.Contains(slot))
            {
                plot.TargetCharacters.Add(newCharacter);
            }
        }

        return newCharacter;
    }

    public async Task DeclineByMaster(ClaimIdentification claimId, ClaimDenialReason claimDenialStatus, string commentText, bool deleteCharacter)
    {
        var (claim, projectInfo) = await LoadClaimForApprovalDecline(claimId);

        var statusWasApproved = claim.ClaimStatus == ClaimStatus.Approved;

        claim.EnsureCanChangeStatus(ClaimStatus.DeclinedByMaster);

        claim.MasterDeclinedDate = Now;
        claim.ClaimStatus = ClaimStatus.DeclinedByMaster;
        claim.ClaimDenialStatus = claimDenialStatus;
        claim.PlayerAllowedSenstiveData = false; // Сбрасываем это при отклонении заявки, если заявку восстановить, надо будет повторно получать разрешение

        var roomEmail = await CommonClaimDecline(claim);

        if (deleteCharacter)
        {
            if (!statusWasApproved)
            {
                throw new InvalidOperationException("Attempt to delete character, but it not exists");
            }
            DeleteCharacter(claim.Character, projectInfo);
        }

        await accommodationInviteService.DeclineAllClaimInvites(claimId);

        var (_, email) = CommentHelper.AddClaimCommentWithNotification(commentText, claim, projectInfo, CommentExtraAction.DeclineByMaster, ClaimOperationType.MasterVisibleChange, Now);

        await UnitOfWork.SaveChangesAsync();
        await claimNotificationService.SendNotification(email);
        if (roomEmail != null)
        {
            await EmailService.Email(roomEmail);
        }
    }

    private void DeleteCharacter(Character character, ProjectInfo projectInfo)
    {

        _ = projectInfo.RequestMasterAccess(currentUserAccessor.UserIdentification, Permission.CanEditRoles);
        MarkTreeModified(character.Project);

        character.DirectlyRelatedPlotElements.CleanLinksList();

        character.IsActive = false;
        MarkChanged(character);
    }

    private async Task<LeaveRoomEmail?> CommonClaimDecline(Claim claim)
    {
        MarkCharacterChangedIfApproved(claim);


        if (claim.Character?.ApprovedClaim == claim)
        {
            claim.Character.ApprovedClaimId = null;
        }

        return await ConsiderLeavingRoom(claim);
    }

    private async Task<LeaveRoomEmail?> ConsiderLeavingRoom(Claim claim)
    {
        LeaveRoomEmail? email = null;

        if (claim.AccommodationRequest != null)
        {
            if (claim.AccommodationRequest.Accommodation != null)
            {
                email = new LeaveRoomEmail()
                {
                    Changed = new[] { claim },
                    Initiator = await GetCurrentUser(),
                    ProjectName = claim.Project.ProjectName,
                    Recipients = claim.AccommodationRequest.Accommodation.GetSubscriptions().ToList(),
                    Room = claim.AccommodationRequest.Accommodation,
                    Text = new MarkdownString(),
                };
            }

            _ = claim.AccommodationRequest.Subjects.Remove(claim);
            if (!claim.AccommodationRequest.Subjects.Any())
            {
                _ = UnitOfWork.GetDbSet<AccommodationRequest>().Remove(claim.AccommodationRequest);
            }
        }

        return email;
    }

    /// <inheritdoc />
    public async Task<AccommodationRequest?> LeaveAccommodationGroupAsync(int projectId, int claimId)
    {
        var claim = await ClaimsRepository.GetClaim(projectId, claimId);

        claim = claim.RequestAccess(currentUserAccessor.UserIdentification,
            Permission.CanSetPlayersAccommodations,
            claim.ClaimStatus == ClaimStatus.Approved
                ? ExtraAccessReason.PlayerOrResponsible
                : ExtraAccessReason.None);

        var acr = claim.AccommodationRequest;
        if (acr is null)
        {
            return null;
        }
        if (acr.Subjects.Count == 1)
        {
            return acr;
        }

        var email = EmailHelpers.CreateFieldsEmailWithExtraData(
            claim,
            s => s.AccommodationChange,
            await GetCurrentUser(),
            [],
            "Тип поселения", //TODO[Localize]
            new PreviousAndNewValue("без поселения", acr.AccommodationType.Name)
            );
        var leaveEmail = await ConsiderLeavingRoom(claim);

        acr.Subjects.Remove(claim);
        claim.AccommodationRequest_Id = null;
        claim.AccommodationRequest = null;
        UnitOfWork.GetDbSet<AccommodationRequest>()
            .Add(
                new AccommodationRequest
                {
                    ProjectId = projectId,
                    AccommodationTypeId = acr.AccommodationTypeId,
                    IsAccepted = AccommodationRequest.InviteState.Accepted,
                    Subjects = [claim]
                });

        await UnitOfWork.SaveChangesAsync();

        await EmailService.Email(email);
        if (leaveEmail is not null)
        {
            await EmailService.Email(leaveEmail);
        }

        return acr;
    }

    public async Task<AccommodationRequest> SetAccommodationType(int projectId,
        int claimId,
        int roomTypeId)
    {
        //todo set first state to Unanswered
        var claim = await ClaimsRepository.GetClaim(projectId, claimId).ConfigureAwait(false);

        claim = claim.RequestAccess(currentUserAccessor.UserIdentification,
            Permission.CanSetPlayersAccommodations,
            claim?.ClaimStatus == ClaimStatus.Approved
                ? ExtraAccessReason.PlayerOrResponsible
                : ExtraAccessReason.None);

        // Player cannot change accommodation type if already checked in

        if (claim.AccommodationRequest?.AccommodationTypeId == roomTypeId)
        {
            return claim.AccommodationRequest;
        }

        var newType = await UnitOfWork.GetDbSet<ProjectAccommodationType>().FindAsync(roomTypeId)
            .ConfigureAwait(false);

        if (newType == null)
        {
            throw new JoinRpgEntityNotFoundException(roomTypeId,
                nameof(ProjectAccommodationType));
        }

        var email = EmailHelpers.CreateFieldsEmailWithExtraData(claim,
            s => s.AccommodationChange,
            await GetCurrentUser(),
            [],
            "Тип поселения", //TODO[Localize]
            new PreviousAndNewValue(newType.Name, claim.AccommodationRequest?.AccommodationType.Name)
            );


        var leaveEmail = await ConsiderLeavingRoom(claim);

        // TODO: Just change accommodation type if this claim is the only occupant of previous room
        var accommodationRequest = new AccommodationRequest
        {
            ProjectId = projectId,
            Subjects = [claim],
            AccommodationTypeId = roomTypeId,
            IsAccepted = AccommodationRequest.InviteState.Accepted,
        };

        _ = UnitOfWork
            .GetDbSet<AccommodationRequest>()
            .Add(accommodationRequest);
        await UnitOfWork.SaveChangesAsync().ConfigureAwait(false);

        await EmailService.Email(email);
        if (leaveEmail != null)
        {
            await EmailService.Email(leaveEmail);
        }

        return accommodationRequest;
    }


    public async Task DeclineByPlayer(ClaimIdentification claimId, string commentText)
    {
        var (claim, projectInfo) = await LoadClaimAsPlayer(claimId);

        claim.EnsureCanChangeStatus(ClaimStatus.DeclinedByUser);

        claim.PlayerDeclinedDate = Now;
        claim.ClaimStatus = ClaimStatus.DeclinedByUser;
        claim.PlayerAllowedSenstiveData = false; // Сбрасываем это при отклонении заявки, если заявку восстановить, надо будет повторно получать разрешение


        await accommodationInviteService.DeclineAllClaimInvites(claimId).ConfigureAwait(false);

        var roomEmail = await CommonClaimDecline(claim);



        var (_, email) = CommentHelper.AddClaimCommentWithNotification(commentText, claim, projectInfo, CommentExtraAction.DeclineByPlayer, ClaimOperationType.PlayerChange, Now);

        await UnitOfWork.SaveChangesAsync();
        await claimNotificationService.SendNotification(email);
        if (roomEmail != null)
        {
            await EmailService.Email(roomEmail);
        }
    }


    public async Task RestoreByMaster(ClaimIdentification claimId, string commentText, CharacterIdentification characterId)
    {
        var (claim, projectInfo) = await LoadClaimForApprovalDecline(claimId);

        var oldCharacterId = claim.GetCharacterId(); // Сохраняем на случай если он изменится
        var character = await CharactersRepository.GetCharacterAsync(characterId);

        claim.EnsureCanChangeStatus(ClaimStatus.AddedByUser);
        claim.ClaimStatus = ClaimStatus.AddedByUser; //TODO: Actually should be "AddedByMaster" but we don't support it yet.
        claim.ClaimDenialStatus = null;
        SetDiscussed(claim, true);

        if (character.ApprovedClaim is not null)
        {
            // Персонаж, куда мы пытаемся восстановить заявку, уже занят.
            throw new ClaimTargetIsNotAcceptingClaims();
        }

        claim.Character = character;
        claim.CharacterId = characterId.Id;

        //Ensure that character is active
        claim.Character.IsActive = true;
        MarkChanged(claim.Character);

        var (_, email) = CommentHelper.AddClaimCommentWithNotification(commentText, claim, projectInfo, CommentExtraAction.RestoreByMaster, ClaimOperationType.MasterVisibleChange, Now);

        email = email with { AnotherCharacterId = oldCharacterId };

        await UnitOfWork.SaveChangesAsync();
        await claimNotificationService.SendNotification(email);
    }

    public async Task MoveByMaster(ClaimIdentification claimId, string commentText, CharacterIdentification characterId)
    {
        var (claim, projectInfo) = await LoadClaimForApprovalDecline(claimId);
        var source = await CharactersRepository.GetCharacterAsync(characterId);
        var userInfo = await UserRepository.GetRequiredUserInfo(new UserIdentification(claim.PlayerUserId));

        var oldCharacterId = claim.GetCharacterId(); // Сохраняем, так как он изменится



        source.EnsureCanMoveClaim(claim, userInfo, projectInfo);

        MarkCharacterChangedIfApproved(claim); // before move

        if (claim.Character != null && claim.IsApproved)
        {
            claim.Character.ApprovedClaim = null;
        }
        claim.CharacterId = characterId.CharacterId;
        claim.Character = source; //That fields is required later

        if (claim.IsApproved)
        {
            claim.Character.ApprovedClaim = claim;
        }

        MarkCharacterChangedIfApproved(claim); // after move

        var (_, email) = CommentHelper.AddClaimCommentWithNotification(commentText, claim, projectInfo, CommentExtraAction.MoveByMaster, ClaimOperationType.MasterVisibleChange, Now);

        email = email with { AnotherCharacterId = oldCharacterId };


        await UnitOfWork.SaveChangesAsync();
        await claimNotificationService.SendNotification(email);
    }

    public async Task UpdateReadCommentWatermark(int projectId, int commentDiscussionId, int maxCommentId)
    {
        var watermarks =
          UnitOfWork.GetDbSet<ReadCommentWatermark>()
            .Where(w => w.CommentDiscussionId == commentDiscussionId && w.UserId == CurrentUserId)
            .OrderByDescending(wm => wm.ReadCommentWatermarkId)
            .ToList();

        //Sometimes watermarks can duplicate. If so, let's remove them.
        foreach (var wm in watermarks.Skip(1))
        {
            _ = UnitOfWork.GetDbSet<ReadCommentWatermark>().Remove(wm);
        }

        var watermark = watermarks.FirstOrDefault();

        if (watermark == null)
        {
            watermark = new ReadCommentWatermark()
            {
                CommentDiscussionId = commentDiscussionId,
                ProjectId = projectId,
                UserId = CurrentUserId,
            };
            _ = UnitOfWork.GetDbSet<ReadCommentWatermark>().Add(watermark);
        }

        if (watermark.CommentId > maxCommentId)
        {
            return;
        }
        watermark.CommentId = maxCommentId;
        await UnitOfWork.SaveChangesAsync();
    }

    public async Task SetResponsible(ClaimIdentification claimId, UserIdentification responsibleMasterId)
    {
        var (claim, projectInfo) = await LoadClaimForApprovalDecline(claimId);

        _ = projectInfo.RequestMasterAccess(responsibleMasterId);

        var oldResponsibleMaster = new UserIdentification(claim.ResponsibleMasterUserId);

        if (responsibleMasterId == oldResponsibleMaster)
        {
            return; // Just do nothing
        }
        claim.ResponsibleMasterUserId = responsibleMasterId;

        var newMaster = await UserRepository.GetById(responsibleMasterId);

        var (_, email) = CommentHelper.AddClaimCommentWithNotification(
            $"{claim.ResponsibleMasterUser.GetDisplayName()} → {newMaster.GetDisplayName()}",
            claim,
            projectInfo,
            CommentExtraAction.ChangeResponsible,
            ClaimOperationType.MasterVisibleChange, Now);

        email = email with { OldResponsibleMaster = oldResponsibleMaster };

        await UnitOfWork.SaveChangesAsync();

        await claimNotificationService.SendNotification(email);
    }

    public async Task SaveFieldsFromClaim(
        ClaimIdentification claimId,
        IReadOnlyDictionary<int, string?> newFieldValue)
    {
        var (claim, projectInfo) = await LoadClaimAsMaster(claimId, Permission.None, ExtraAccessReason.Player);

        var updatedFields = fieldSaveHelper.SaveCharacterFields(CurrentUserId, claim, newFieldValue, projectInfo);
        if (updatedFields.Any(f => f.Field.BoundTo == FieldBoundTo.Character) && claim.Character != null)
        {
            MarkChanged(claim.Character);
        }
        var user = await GetCurrentUser();
        var email = EmailHelpers.CreateFieldsEmail(claim, s => s.FieldChange, user, updatedFields);

        await UnitOfWork.SaveChangesAsync();

        await EmailService.Email(email);
    }

    public async Task OnHoldByMaster(ClaimIdentification claimId, string commentText)
    {

        var (claim, projectInfo) = await LoadClaimForApprovalDecline(claimId);

        MarkCharacterChangedIfApproved(claim);
        claim.ChangeStatusWithCheck(ClaimStatus.OnHold);


        var (_, email) = CommentHelper.AddClaimCommentWithNotification(commentText, claim, projectInfo, CommentExtraAction.OnHoldByMaster, ClaimOperationType.MasterVisibleChange, Now);


        await UnitOfWork.SaveChangesAsync();
        await claimNotificationService.SendNotification(email);
    }

    private void MarkCharacterChangedIfApproved(Claim claim)
    {
        if (claim.ClaimStatus == ClaimStatus.Approved && claim.Character != null)
        {
            MarkChanged(claim.Character);
        }
    }

    private void SetDiscussed(Claim claim, bool isVisibleToPlayer)
    {
        claim.LastUpdateDateTime = Now;
        if (claim.ClaimStatus == ClaimStatus.AddedByMaster && CurrentUserId == claim.PlayerUserId)
        {
            claim.ClaimStatus = ClaimStatus.Discussed;
        }

        if (claim.ClaimStatus == ClaimStatus.AddedByUser && CurrentUserId != claim.PlayerUserId && isVisibleToPlayer)
        {
            claim.ClaimStatus = ClaimStatus.Discussed;
        }
    }

    public async Task ConcealComment(int projectId,
        int commentId,
        int commentDiscussionId,
        int currentUserId)
    {
        var discussion = await ForumRepository.GetDiscussionByComment(projectId, commentId);
        var childComments = discussion.Comments.Where(c => c.ParentCommentId == commentId);
        var comment = discussion.Comments.FirstOrDefault(coment => coment.CommentId == commentId);

        if (comment == null)
        {
            throw new JoinRpgEntityNotFoundException(commentId, nameof(Comment));
        }

        if (comment.HasMasterAccess(currentUserId) && !childComments.Any() &&
            comment.IsVisibleToPlayer && !comment.IsCommentByPlayer)
        {
            comment.IsVisibleToPlayer = false;
            await UnitOfWork.SaveChangesAsync();
        }
        else
        {
            throw new JoinRpgConcealCommentException();
        }
    }

    public async Task AllowSensitiveData(ClaimIdentification claimId)
    {
        var (claim, _) = await LoadClaimAsPlayer(claimId);

        claim.PlayerAllowedSenstiveData = true;
        await UnitOfWork.SaveChangesAsync();
    }

    public async Task<ClaimIdentification> SystemEnsureClaim(ProjectIdentification donateProjectId)
    {
        var claims = await ClaimsRepository.GetClaimsForPlayer(donateProjectId, currentUserAccessor.UserIdentification, ClaimStatusSpec.Any);
        var claim = claims.TrySelectSingleClaim() ?? claims.FirstOrDefault();
        if (claim != null)
        {
            //TODO восстановить заявку, если она была отозвана или отклонена
            return claim.GetId();
        }
        var projectInfo = await ProjectMetadataRepository.GetProjectMetadata(donateProjectId);
        if (projectInfo.DefaultTemplateCharacter is null)
        {
            logger.LogError("Некорректно настроен проект донатов {donateProjectId}", donateProjectId);
            throw new JoinRpgProjectMisconfiguredException(donateProjectId, "У проекта должен быть шаблон по умолчанию");
        }
        return await AddClaimFromUser(projectInfo.DefaultTemplateCharacter, claimText: "", fields: new Dictionary<int, string?>(), sensitiveDataAllowed: false);
    }

    public static void SetParentCommentAndCheck((Comment, ClaimSimpleChangedNotification) result, Comment parentComment, ClaimOperationType claimOperationType)
    {
        if (claimOperationType != ClaimOperationType.MasterSecretChange && parentComment.IsVisibleToPlayer == false)
        {
            throw new EntityWrongStatusException(parentComment); // Нельзя ответить на скрытый комментарий так, чтобы игрок видел
        }

        result.Item1.Parent = parentComment;
        result.Item2 = result.Item2 with
        {
            ParentCommentAuthor = parentComment.Author.ToUserInfoHeader(),
            PaymentOwner = parentComment?.Finance?.PaymentType?.User?.ToUserInfoHeader(),
        };
    }

    private async Task<(Claim, ProjectInfo)> LoadClaimForApprovalDecline(ClaimIdentification claimId)
    {
        return await LoadClaimAsMaster(claimId, Permission.CanManageClaims, ExtraAccessReason.ResponsibleMaster);
    }
}

