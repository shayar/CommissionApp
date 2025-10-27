using EPC.Application.Services;
using EPC.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using EPC.Domain.Entities;
using EPC.Application.Dtos;
using EPC.Infrastructure.Data;

namespace EPC.WEB.Pages
{
    [Authorize] // Requires login for any dashboard access
    public class DashboardModel : PageModel
    {
        private readonly IDashboardService _dashboardService; // Use the interface
        private readonly UserManager<AppUser> _userManager;
        private readonly Serilog.ILogger _logger;
        private readonly AppDbContext _context;

        public DashboardModel(IDashboardService dashboardService, UserManager<AppUser> userManager,AppDbContext context, Serilog.ILogger logger)
        {
            _dashboardService = dashboardService;
            _userManager = userManager;
            _context = context;
            _logger = logger.ForContext<DashboardModel>();
        }

        public bool IsAdmin { get; set; }
        public string FullName { get; set; } = "Employee";

        // Employee Dashboard Data
        public EmployeeSummaryDto? EmployeeSummary { get; set; }

        // Admin Dashboard Data
        public List<EmployeePerformanceDto> TopEmployees { get; set; } = new List<EmployeePerformanceDto>();
        public List<CategorySalesDto> CategorySales { get; set; } = new List<CategorySalesDto>();

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.Warning("Unauthorized access attempt to dashboard.");
                return RedirectToPage("/Account/Login");
            }

            FullName = user.FullName ?? user.Email ?? "User";
            IsAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            var currentMonthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            if (IsAdmin)
            {
                _logger.Information("Admin user {Email} viewing dashboard.", user.Email);

                // ADMIN LOGIC: Load performance aggregates and category sales using the service
                TopEmployees = await _dashboardService.GetTopEmployeePerformanceAsync(currentMonthStart);
                CategorySales = await _dashboardService.GetSalesByCategoryAsync(currentMonthStart);

                // Fetch full names for top employees (Presentation/UI logic)
                foreach (var emp in TopEmployees)
                {
                    var empUser = await _userManager.FindByIdAsync(emp.AppUserId);
                    emp.EmployeeFullName = empUser?.FullName ?? $"ID: {empUser?.EmployeeId}";
                }
            }
            else // Employee
            {
                _logger.Information("Employee user {Email} viewing dashboard.", user.Email);

                // EMPLOYEE LOGIC: Load personal summary using the service
                EmployeeSummary = await _dashboardService.GetEmployeeSummaryAsync(user.Id, currentMonthStart);
            }

            // --- Business Logic for Developer Kill-Switch (Audit and LastLogin Update) ---

            if (user.LastLogin.GetValueOrDefault(DateTime.MinValue) < DateTime.UtcNow.AddMinutes(-1))
            {
                user.LastLogin = DateTime.UtcNow;
                var updateResult = await _userManager.UpdateAsync(user);

                if (updateResult.Succeeded)
                {
                    // Audit successful login update (important for kill-switch tracking)
                    var auditEntry = new Audit
                    {
                        Action = $"User login/activity: {user.Email}",
                        PerformedBy = user.Email
                    };
                    _context.AuditLogs.Add(auditEntry);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    _logger.Error("Failed to update LastLogin for user {Email}: {Errors}", user.Email, string.Join(", ", updateResult.Errors.Select(e => e.Description)));
                }
            }

            return Page();
        }
    }
}