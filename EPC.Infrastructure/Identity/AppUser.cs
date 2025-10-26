using EPC.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace EPC.Infrastructure.Identity
{
    public class AppUser : IdentityUser
    {
        public string? EmployeeId { get; set; }
        public string? FullName { get; set; }
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public DateTime? JoiningDate { get; set; }
        public string? StoreId { get; set; }
        public DateTime? LastLogin { get; set; }
        public List<Sale> Sales { get; set; }
    }
}
