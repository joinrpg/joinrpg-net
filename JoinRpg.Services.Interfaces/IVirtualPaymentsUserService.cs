using System.Threading.Tasks;
using JoinRpg.DataModel;

namespace JoinRpg.Services.Interfaces
{
    /// <summary>
    /// Interface for singleton service that provides virtual payments user
    /// </summary>
    public interface IVirtualPaymentsUserService
    {
        /// <summary>
        /// Data object of virtual payments manager user
        /// </summary>
        Task<User> UserAsync { get; }
    }
}
