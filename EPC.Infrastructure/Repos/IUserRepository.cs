using EPC.Infrastructure.Identity;

namespace EPC.Infrastructure.Repos
{
    public interface IUserRepository
    {
        Task<AppUser?> GetAppUserByIdAsync(string appUserId);
    }
}
