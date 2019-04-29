namespace JoinRpg.Dal.Impl
{
    /// <summary>
    /// Provides configuration for DbContext
    /// </summary>
    public interface IJoinDbContextConfiguration
    {
        /// <summary>
        /// Connection String
        /// </summary>
        string ConnectionString { get; }
    }
}
