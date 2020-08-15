namespace JoinRpg.Web.Models.FieldSetup
{
    /// <summary>
    /// Navigation in fields area
    /// </summary>
    public class FieldNavigationModel : IProjectIdAware
    {
        /// <see cref="FieldNavigationPage"/>
        public FieldNavigationPage Page { get; set; }

        /// <inheritdoc />
        public int ProjectId { get; set; }

        /// <summary>
        /// Can current user edit fields
        /// </summary>
        public bool CanEditFields { get; set; }
    }

    /// <summary>
    /// Chosen page
    /// </summary>
    public enum FieldNavigationPage
    {
        /// <summary>
        /// Some unknown page of this area
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// All active fields
        /// </summary>
        ActiveFieldsList = 1,
        /// <summary>
        /// All deleted fields
        /// </summary>
        DeletedFieldsList = 2,
        /// <summary>
        /// Field settings
        /// </summary>
        FieldSettings = 3,
        /// <summary>
        /// Adding field
        /// </summary>
        AddField = 4,
        /// <summary>
        /// Editing field
        /// </summary>
        EditField = 5,
    }
}
