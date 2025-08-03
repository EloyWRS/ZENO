using ZENO_API_II.Models;

namespace ZENO_API_II.Services.Interfaces
{
    public interface IUserSetupService
    {
        Task SetupNewUserAsync(UserLocal user);
    }
} 