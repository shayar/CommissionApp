using EPC.Domain.Entities;
using EPC.Infrastructure.Data;
using EPC.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography.X509Certificates;


namespace EPC.WEB.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _context;


        public LoginModel(
        SignInManager<AppUser> signInManager,
        UserManager<AppUser> userManager,
        AppDbContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
        }


        [BindProperty] public InputModel Input { get; set; }


        public string ReturnUrl { get; set; }
        public string StoreName { get; set; }


        public class InputModel
        {
            [Required]
            public string StoreId { get; set; }


            [Required]
            public string EmployeeOrEmail { get; set; }


            [Required, DataType(DataType.Password)]
            public string Password { get; set; }
        }


        public async Task<IActionResult> OnGetAsync(string returnUrl = null)
        {
            if (User.Identity.IsAuthenticated)
            {
                Log.Information("Authenticated user redirected from login page.");
                return RedirectToPage("/Dashboard");
            }

            ReturnUrl = returnUrl ?? Url.Content("~/");


            // Redirect to setup if no store is configured
            var store = await _context.Stores.FirstOrDefaultAsync();
            if (store == null)
                return RedirectToPage("/Index");

            StoreName = store.Name;
            return Page();
        }




        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");
            if (!ModelState.IsValid)
            {
                var store = await _context.Stores.FirstOrDefaultAsync();
                StoreName = store?.Name ?? "Application";
                return Page();
            }


            try
            {
                // Validate store exists
                var storeExists = await _context.Stores.AnyAsync(s => s.StoreId == Input.StoreId);
                if (!storeExists)
                {
                    ModelState.AddModelError(string.Empty, "Invalid Store ID.");
                    return Page();
                }


                // Find user by email or employee ID and store match
                var user = await _userManager.Users.FirstOrDefaultAsync(u =>
                u.StoreId == Input.StoreId &&
                (u.Email == Input.EmployeeOrEmail || u.EmployeeId == Input.EmployeeOrEmail));


                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Invalid credentials.");
                    return Page();
                }


                if (await _userManager.IsLockedOutAsync(user))
                {
                    ModelState.AddModelError(string.Empty, "Your account is locked. Contact your admin.");
                    return Page();
                }


                var result = await _signInManager.PasswordSignInAsync(user.UserName, Input.Password, false, true);
                if (result.Succeeded)
                {
                    user.LastLogin = DateTime.UtcNow;
                    await _userManager.UpdateAsync(user);

                    Log.Information("{Email} logged into store {StoreId}", user.Email, Input.StoreId);
                    _context.AuditLogs.Add(new Audit
                    {
                        Action = $"Login by {user.Email}",
                        PerformedBy = user.Email
                    });
                    await _context.SaveChangesAsync();
                    return RedirectToPage("/Dashboard");
                }


                if (result.IsLockedOut)
                {
                    Log.Warning("User {Email} locked out due to failed login attempts", user.Email);
                    ModelState.AddModelError(string.Empty, "Account locked. Contact your admin.");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid credentials.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Login failed for user {User}", Input.EmployeeOrEmail);
                ModelState.AddModelError(string.Empty, "Login failed. Please try again later.");
            }
            var failedStore = await _context.Stores.FirstOrDefaultAsync();
            StoreName = failedStore?.Name ?? "Application";

            return Page();
        }


    }
}