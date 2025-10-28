using EPC.Domain.Entities;
using EPC.Infrastructure.Data;
using EPC.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.ComponentModel.DataAnnotations;
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EPC.WEB.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly AppDbContext _context;
        private readonly ILogger<RegisterModel> _logger;

        public RegisterModel(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, AppDbContext context, ILogger<RegisterModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _logger = logger;
        }

        [BindProperty] public InputModel Input { get; set; }
        public string StoreName { get; set; }
        public string ReturnUrl { get; set; }

        public class InputModel
        {
            [Required] public string StoreId { get; set; }

            [Required] public string EmployeeId { get; set; }

            [Required] public string FullName { get; set; }

            [Required] public string Gender { get; set; }

            [Required, DataType(DataType.Date)]
            public DateTime DateOfBirth { get; set; }

            [Required, DataType(DataType.Date)]
            public DateTime JoiningDate { get; set; }

            [Required, EmailAddress]
            public string Email { get; set; }

            [Required, DataType(DataType.Password)]
            public string Password { get; set; }
        }

        public List<SelectListItem> StoreOptions { get; set; }

        public async Task<IActionResult> OnGetAsync(string returnUrl = null)
        {
            if (User.Identity.IsAuthenticated)
            {
                Log.Information("Authenticated user redirected from register page.");
                return RedirectToPage("/Dashboard");
            }
            ReturnUrl = returnUrl ?? Url.Content("~/");

            var store = await _context.Stores.FirstOrDefaultAsync();
            if (store == null)
                return RedirectToPage("/Index");

            StoreName = store.Name;

            // Load store options for dropdown
            StoreOptions = await _context.Stores
                .Select(s => new SelectListItem { Value = s.StoreId, Text = s.Name })
                .ToListAsync();

            return Page();
        }


        public async Task<IActionResult> OnPostAsync()
        {
            StoreOptions = await _context.Stores
                .Select(s => new SelectListItem { Value = s.StoreId, Text = s.Name })
                .ToListAsync();

            if (!ModelState.IsValid)
                return Page();

            try
            {
                var existingUser = await _userManager.Users.FirstOrDefaultAsync(u => u.Email == Input.Email || u.EmployeeId == Input.EmployeeId);
                if (existingUser != null)
                {
                    ModelState.AddModelError(string.Empty, "Email or Employee ID is already registered.");
                    return Page();
                }

                var user = new AppUser
                {
                    UserName = Input.Email,
                    Email = Input.Email,
                    StoreId = Input.StoreId,
                    EmployeeId = Input.EmployeeId,
                    FullName = Input.FullName,
                    Gender = Input.Gender,
                    DateOfBirth = Input.DateOfBirth,
                    JoiningDate = Input.JoiningDate
                };

                var result = await _userManager.CreateAsync(user, Input.Password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Employee");
                    await _signInManager.SignInAsync(user, false);

                    _context.AuditLogs.Add(new Audit
                    {
                        Action = $"User registered: {Input.Email}",
                        PerformedBy = Input.Email
                    });
                    await _context.SaveChangesAsync();

                    Log.Information("User registered: {Email}, Store: {StoreId}, EmployeeId: {EmployeeId}", Input.Email, Input.StoreId, Input.EmployeeId);

                    return RedirectToPage("/Dashboard");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                    Log.Error("User registration error for {Email}: {Error}", Input.Email, error.Description);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unhandled exception during registration for {Email}", Input.Email);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again later.");
            }

            return Page();
        }
    }
}
