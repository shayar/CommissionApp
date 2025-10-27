// EPC.WEB/Pages/Admin/CategoriesModel.cshtml.cs (Full Code)

using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EPC.Domain.Entities;
using EPC.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using EPC.Application.Services;
using EPC.Application.Dtos;

namespace EPC.WEB.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class CategoriesModel : PageModel
    {
        private readonly ICategoryManagementService _categoryService;
        private readonly UserManager<AppUser> _userManager;
        private readonly Serilog.ILogger _logger;

        public CategoriesModel(ICategoryManagementService categoryService, UserManager<AppUser> userManager, Serilog.ILogger logger)
        {
            _categoryService = categoryService;
            _userManager = userManager;
            _logger = logger.ForContext<CategoriesModel>();
        }

        public CategoryCommand CategoryInput { get; set; } = new CategoryCommand();
        public SubCategoryCommand SubCategoryInput { get; set; } = new SubCategoryCommand();

        public List<Category> Categories { get; set; } = new List<Category>();

        [TempData] public string SuccessMessage { get; set; } = string.Empty;
        [TempData] public string ErrorMessage { get; set; } = string.Empty;

        private async Task LoadDataAndUser()
        {
            try
            {
                Categories = await _categoryService.GetAllCategoriesAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to load categories for Admin page.");
                // Set ErrorMessage here for load failure
                ErrorMessage = "Failed to load existing categories due to a system error.";
                Categories = new List<Category>();
            }
        }

        public async Task OnGetAsync()
        {
            // Initialize Command objects for modal use on GET
            CategoryInput = new CategoryCommand();
            SubCategoryInput = new SubCategoryCommand();
            await LoadDataAndUser();
        }

        private async Task<string> GetAuditUserIdentifier()
        {
            var user = await _userManager.GetUserAsync(User);
            return user?.Email ?? user?.Id ?? "System/Unknown";
        }

        // ------------------------- CATEGORY HANDLERS -------------------------

        public async Task<IActionResult> OnPostAddCategoryAsync(CategoryCommand CategoryInput)
        {
            // Validation check (uses the CategoryInput parameter)
            if (!ModelState.IsValid)
            {
                this.CategoryInput = CategoryInput; // Restore input
                await LoadDataAndUser();
                return Page();
            }

            var performedBy = await GetAuditUserIdentifier();

            try
            {
                await _categoryService.AddCategoryAsync(CategoryInput, performedBy);
                SuccessMessage = $"Category '{CategoryInput.Name}' added successfully.";
                return RedirectToPage();
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(nameof(CategoryInput) + "." + nameof(CategoryInput.Name), ex.Message);
                ErrorMessage = ex.Message; // Business logic error for red banner
            }
            catch (Exception ex)
            {
                ErrorMessage = "An unexpected error occurred while adding the category.";
                _logger.Error(ex, "Error adding category.");
            }

            this.CategoryInput = CategoryInput; // Restore input on failure
            await LoadDataAndUser();
            return Page();
        }

        public async Task<IActionResult> OnPostUpdateCategoryAsync(CategoryCommand CategoryInput)
        {
            if (!ModelState.IsValid)
            {
                this.CategoryInput = CategoryInput;
                await LoadDataAndUser();
                return Page();
            }

            var performedBy = await GetAuditUserIdentifier();

            try
            {
                await _categoryService.UpdateCategoryAsync(CategoryInput, performedBy);
                SuccessMessage = $"Category '{CategoryInput.Name}' updated successfully.";
                return RedirectToPage();
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(nameof(CategoryInput) + "." + nameof(CategoryInput.Name), ex.Message);
                ErrorMessage = ex.Message;
            }
            catch (KeyNotFoundException ex)
            {
                ErrorMessage = ex.Message;
                _logger.Warning("Attempted to update non-existent category ID {Id}.", CategoryInput.Id);
            }
            catch (Exception ex)
            {
                ErrorMessage = "An unexpected error occurred while updating the category.";
                _logger.Error(ex, "Error updating category ID {Id}.", CategoryInput.Id);
            }

            this.CategoryInput = CategoryInput;
            await LoadDataAndUser();
            return Page();
        }

        // ------------------------- SUB-CATEGORY HANDLERS -------------------------

        public async Task<IActionResult> OnPostAddSubCategoryAsync(SubCategoryCommand SubCategoryInput)
        {
            if (!ModelState.IsValid)
            {
                this.SubCategoryInput = SubCategoryInput; // Restore input
                await LoadDataAndUser();
                return Page();
            }

            var performedBy = await GetAuditUserIdentifier();

            try
            {
                await _categoryService.AddSubCategoryAsync(SubCategoryInput, performedBy);
                SuccessMessage = $"SubCategory '{SubCategoryInput.Name}' added successfully.";
                return RedirectToPage();
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(nameof(SubCategoryInput) + "." + nameof(SubCategoryInput.Name), ex.Message);
                ErrorMessage = ex.Message;
            }
            catch (KeyNotFoundException ex)
            {
                ModelState.AddModelError(nameof(SubCategoryInput) + "." + nameof(SubCategoryInput.CategoryId), ex.Message);
                ErrorMessage = ex.Message;
            }
            catch (Exception ex)
            {
                ErrorMessage = "An unexpected error occurred while adding the subcategory.";
                _logger.Error(ex, "Error adding subcategory.");
            }

            this.SubCategoryInput = SubCategoryInput;
            await LoadDataAndUser();
            return Page();
        }

        public async Task<IActionResult> OnPostUpdateSubCategoryAsync(SubCategoryCommand SubCategoryInput)
        {
            if (!ModelState.IsValid)
            {
                this.SubCategoryInput = SubCategoryInput;
                await LoadDataAndUser();
                return Page();
            }

            var performedBy = await GetAuditUserIdentifier();

            try
            {
                await _categoryService.UpdateSubCategoryAsync(SubCategoryInput, performedBy);
                SuccessMessage = $"SubCategory '{SubCategoryInput.Name}' updated successfully.";
                return RedirectToPage();
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(nameof(SubCategoryInput) + "." + nameof(SubCategoryInput.Name), ex.Message);
                ErrorMessage = ex.Message;
            }
            catch (KeyNotFoundException ex)
            {
                ModelState.AddModelError(nameof(SubCategoryInput) + "." + nameof(SubCategoryInput.Id), ex.Message);
                ErrorMessage = ex.Message;
            }
            catch (Exception ex)
            {
                ErrorMessage = "An unexpected error occurred while updating the subcategory.";
                _logger.Error(ex, "Error updating subcategory with ID {Id}.", SubCategoryInput.Id);
            }

            this.SubCategoryInput = SubCategoryInput;
            await LoadDataAndUser();
            return Page();
        }
    }
}