namespace JoinRpg.Services.Interfaces
{
    public class AccessRequestBase
    {
        public int ProjectId { get; set; }
        public int UserId { get; set; }
        /// <summary>
        /// Ability to grand and edit roles for other members, and give master rights
        /// </summary>
        public bool CanGrantRights { get; set; }
        /// <summary>
        /// Ability to edit project claim's fields and character's fields
        /// </summary>
        public bool CanChangeFields { get; set; }
        /// <summary>
        /// Ability to edit project and it's properties like name, status and etc.
        /// </summary>
        public bool CanChangeProjectProperties { get; set; }
        /// <summary>
        /// Ability to change status of active project's claims (accept, reject, move)
        /// </summary>
        public bool CanManageClaims { get; set; }
        /// <summary>
        /// Ability to edit current project roles (create new role groups, add/remove characters to it)
        /// </summary>
        public bool CanEditRoles { get; set; }
        /// <summary>
        /// Ability to manage project finances
        /// </summary>
        public bool CanManageMoney { get; set; }
        /// <summary>
        /// Ability to send massive mails to project members
        /// </summary>
        public bool CanSendMassMails { get; set; }
        /// <summary>
        /// Ability to edit project plots, sets role groups to it, visibility and etc.
        /// </summary>
        public bool CanManagePlots { get; set; }
        public bool CanManageAccommodation { get; set; }
        public bool CanSetPlayersAccommodations { get; set; }
    }
}
