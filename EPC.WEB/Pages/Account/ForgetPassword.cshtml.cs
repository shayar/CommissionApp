using EPC.Domain.Entities;
using EPC.Infrastructure.Data;
using EPC.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.ComponentModel.DataAnnotations;

namespace EPC.WEB.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _context;

        public ForgotPasswordModel(UserManager<AppUser> userManager, AppDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [BindProperty] public InputModel Input { get; set; }
        public string StatusMessage { get; set; }

        public class InputModel
        {
            [Required] public string EmployeeId { get; set; }

            [Required] public string StoreId { get; set; }

            [Required, EmailAddress] public string Email { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            try
            {
                var user = await _userManager.Users.FirstOrDefaultAsync(u =>
                    u.EmployeeId == Input.EmployeeId &&
                    u.Email == Input.Email &&
                    u.StoreId == Input.StoreId);

                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "No user found with the provided details.");
                    Log.Warning("ForgotPassword: Invalid attempt for EmployeeId={EmployeeId}, StoreId={StoreId}, Email={Email}",
                        Input.EmployeeId, Input.StoreId, Input.Email);
                    return Page();
                }

                // Lock the account for admin reset
                user.LockoutEnabled = true;
                user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
                await _userManager.UpdateAsync(user);

                _context.AuditLogs.Add(new Audit
                {
                    Action = $"Forgot password request submitted by {user.Email}",
                    PerformedBy = user.Email,
                    Timestamp = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();

                StatusMessage = "Your request has been submitted. Please contact your admin for further assistance.";
                Log.Information("ForgotPassword: Lockout applied for user {Email}", user.Email);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error processing forgot password request for {Email}", Input?.Email);
                ModelState.AddModelError(string.Empty, "Something went wrong. Please try again later.");
            }

            return Page();
        }
    }
}
