using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using EPC.Infrastructure.Data;
using EPC.Domain.Entities;
using static System.Formats.Asn1.AsnWriter;
using Microsoft.EntityFrameworkCore;
using Serilog;
using EPC.Infrastructure.Identity;

namespace EPC.WEB.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public IndexModel(ILogger<IndexModel> logger, AppDbContext context, UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }
        [BindProperty]
        public string? StoreCode { get; set; }

        [BindProperty]
        [Required]
        public string StoreName { get; set; }

        [Required]
        [BindProperty] public string StreetAddress1 { get; set; }
        [BindProperty] public string? StreetAddress2 { get; set; }
        [BindProperty][Required] public string City { get; set; }
        [BindProperty][Required] public string State { get; set; }
        [BindProperty][Required] public string ZipCode { get; set; }
        [BindProperty][Required] public string PhoneNumber { get; set; }
        [BindProperty][Required] public string Country { get; set; } = "US";


        [BindProperty, Required, EmailAddress]
        public string AdminEmail { get; set; }


        [BindProperty, Required, DataType(DataType.Password)]
        public string AdminPassword { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var storeExists = await _context.Stores.AnyAsync();
            if (storeExists)
                return RedirectToPage("/Account/Login");

            return Page();
        }


        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                Log.Warning("ModelState is invalid during setup.");
                return Page();
            }

            if (await _context.Stores.AnyAsync())
            {
                ModelState.AddModelError(string.Empty, "Setup has already been completed.");
                Log.Warning("Attempted setup but store already exists.");
                return Page();
            }

            try
            {
                Log.Information("Creating roles if missing...");
                string[] roles = ["Admin", "Employee", "Developer"];
                foreach (var role in roles)
                {
                    if (!await _roleManager.RoleExistsAsync(role))
                    {
                        var roleResult = await _roleManager.CreateAsync(new IdentityRole(role));
                        if (!roleResult.Succeeded)
                        {
                            Log.Error("Failed to create role {Role}: {Errors}", role, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                        }
                        else
                        {
                            Log.Information("Role created: {Role}", role);
                        }
                    }
                }

                Log.Information("Creating admin user: {Email}", AdminEmail);
                var user = new AppUser
                {
                    UserName = AdminEmail,
                    Email = AdminEmail,
                    EmailConfirmed = true,
                    FullName = "Admin",
                    JoiningDate = DateTime.UtcNow,
                    StoreId = StoreCode
                };

                var result = await _userManager.CreateAsync(user, AdminPassword);
                if (!result.Succeeded)
                {
                    foreach (var err in result.Errors)
                    {
                        Log.Error("User creation error: {Error}", err.Description);
                        ModelState.AddModelError(string.Empty, err.Description);
                    }
                    return Page();
                }

                await _userManager.AddToRoleAsync(user, "Admin");
                Log.Information("Admin user created and assigned role successfully.");

                Log.Information("Saving store to database...");
                _context.Stores.Add(new Store
                {
                    StoreId = StoreCode,
                    Name = StoreName,
                    StreetAddress1 = StreetAddress1,
                    StreetAddress2 = StreetAddress2,
                    City = City,
                    State = State,
                    ZipCode = ZipCode,
                    PhoneNumber = PhoneNumber,
                    Country = Country
                });

                _context.AuditLogs.Add(new Audit
                {
                    Action = "Initial store setup",
                    PerformedBy = AdminEmail
                });

                await _context.SaveChangesAsync();
                Log.Information("Store and audit saved successfully.");

                return RedirectToPage("/Account/Login");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Setup failed unexpectedly.");
                ModelState.AddModelError(string.Empty, "Unexpected error occurred. Please check logs.");
                return Page();
            }
        }


    }
}
