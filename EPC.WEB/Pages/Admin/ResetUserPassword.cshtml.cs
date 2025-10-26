using EPC.Domain.Entities;
using EPC.Infrastructure.Data;
using EPC.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.ComponentModel.DataAnnotations;

namespace EPC.WEB.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ResetUserPasswordModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _context;

        public ResetUserPasswordModel(UserManager<AppUser> userManager, AppDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty]
        public List<AppUser> Users { get; set; } = new();

        [BindProperty]
        public ResetPasswordInput Input { get; set; }

        public class ResetPasswordInput
        {
            [Required]
            public string UserId { get; set; }

            [Required, DataType(DataType.Password)]
            public string NewPassword { get; set; }
        }

        public async Task OnGetAsync()
        {
            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                query = query.Where(u =>
                    u.Email.Contains(SearchTerm) ||
                    u.FullName.Contains(SearchTerm) ||
                    u.EmployeeId.Contains(SearchTerm));
            }

            Users = await query.OrderBy(u => u.Email).ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                Users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();
                return Page();
            }

            try
            {
                var user = await _userManager.FindByIdAsync(Input.UserId);
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "User not found.");
                    Log.Warning("Attempted to reset password for non-existent user ID: {UserId}", Input.UserId);
                    return Page();
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, Input.NewPassword);

                if (result.Succeeded)
                {
                    user.LockoutEnd = null;
                    await _userManager.UpdateAsync(user);

                    _context.AuditLogs.Add(new Audit
                    {
                        Action = $"Password reset for {user.Email}",
                        PerformedBy = User.Identity?.Name ?? "System",
                        Timestamp = DateTime.UtcNow
                    });

                    await _context.SaveChangesAsync();
                    Log.Information("Admin {Admin} reset password for user {Email}", User.Identity?.Name, user.Email);
                    TempData["Success"] = $"Password reset for {user.Email}.";
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                        Log.Error("Password reset error for user {Email}: {Error}", user.Email, error.Description);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error during password reset.");
                ModelState.AddModelError(string.Empty, "An unexpected error occurred.");
            }

            Users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();
            return Page();
        }
    }
}
