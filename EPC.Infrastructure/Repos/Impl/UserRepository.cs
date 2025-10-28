using EPC.Infrastructure.Data;
using EPC.Infrastructure.Identity;

namespace EPC.Infrastructure.Repos.Impl
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AppUser?> GetAppUserByIdAsync(string appUserId)
        {
            return await _context.Users.FindAsync(appUserId);
        }
    }
}
