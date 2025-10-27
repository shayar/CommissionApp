using EPC.Application.Dtos;
using EPC.Domain.Entities;
using EPC.Infrastructure.Data;
using EPC.Infrastructure.Repos;
using Serilog;

namespace EPC.Application.Services.Impl
{
    public class CategoryManagementService : ICategoryManagementService
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly AppDbContext _context;
        private readonly ILogger _logger;

        public CategoryManagementService(ICategoryRepository categoryRepository, AppDbContext context, ILogger logger)
        {
            _categoryRepository = categoryRepository;
            _context = context;
            _logger = logger.ForContext<CategoryManagementService>();
        }

        public async Task<List<Category>> GetAllCategoriesAsync()
        {
            _logger.Information("Fetching all categories for admin UI.");
            return await _categoryRepository.GetAllCategoriesWithSubCategoriesAsync();
        }

        // --- Category Handlers ---

        public async Task<Category> AddCategoryAsync(CategoryCommand command, string performedBy)
        {
            if (await _categoryRepository.CategoryNameExistsAsync(command.Name))
            {
                throw new InvalidOperationException($"A category named '{command.Name}' already exists.");
            }

            // BUSINESS RULE: If a rate is provided, it must be the only source of commission.
            if (command.CommissionRate.HasValue && command.CommissionRate.Value <= 0)
            {
                throw new InvalidOperationException("Category commission rate must be greater than zero if set.");
            }

            var category = new Category
            {
                CategoryId = command.CategoryId,
                Name = command.Name,
                CommissionRate = command.CommissionRate // Can be null or a value
            };

            var newCategory = await _categoryRepository.AddCategoryAsync(category);

            _context.AuditLogs.Add(new Audit
            {
                Action = $"Category created: {newCategory.Name} (Rate: {newCategory.CommissionRate:P0})",
                PerformedBy = performedBy
            });
            await _context.SaveChangesAsync();

            _logger.Information("Category '{Name}' added by {User}.", command.Name, performedBy);
            return newCategory;
        }

        public async Task UpdateCategoryAsync(CategoryCommand command, string performedBy)
        {
            var categoryToUpdate = await _categoryRepository.GetCategoryByIdAsync(command.Id);
            if (categoryToUpdate == null)
            {
                throw new KeyNotFoundException($"Category with ID {command.Id} not found.");
            }

            if (await _categoryRepository.CategoryNameExistsAsync(command.Name, command.Id))
            {
                throw new InvalidOperationException($"A category named '{command.Name}' already exists.");
            }

            // BUSINESS RULE: Cannot set a direct rate on a category that HAS subcategories.
            if (categoryToUpdate.SubCategories.Any() && command.CommissionRate.HasValue)
            {
                throw new InvalidOperationException("Cannot set a direct commission rate on a category that contains subcategories. Remove the rate or the subcategories.");
            }

            // Map updates
            categoryToUpdate.Name = command.Name;
            categoryToUpdate.CategoryId = command.CategoryId;
            categoryToUpdate.CommissionRate = command.CommissionRate;

            await _categoryRepository.UpdateCategoryAsync(categoryToUpdate);

            _context.AuditLogs.Add(new Audit
            {
                Action = $"Category updated: {categoryToUpdate.Name}",
                PerformedBy = performedBy
            });
            await _context.SaveChangesAsync();
        }

        // --- SubCategory Handlers ---

        public async Task<SubCategory> AddSubCategoryAsync(SubCategoryCommand command, string performedBy)
        {
            var parentCategory = await _categoryRepository.GetCategoryByIdAsync(command.CategoryId);
            if (parentCategory == null)
            {
                throw new KeyNotFoundException($"Parent Category ID {command.CategoryId} not found.");
            }

            // BUSINESS RULE: Must clear the Category's own rate if adding the first subcategory.
            if (!parentCategory.SubCategories.Any() && parentCategory.CommissionRate.HasValue)
            {
                parentCategory.CommissionRate = null;
                await _categoryRepository.UpdateCategoryAsync(parentCategory);
            }

            if (await _categoryRepository.SubCategoryNameExistsAsync(command.Name, command.CategoryId))
            {
                throw new InvalidOperationException($"A subcategory named '{command.Name}' already exists in this category.");
            }

            if (command.CommissionRate <= 0)
            {
                throw new InvalidOperationException("Subcategory commission rate must be greater than zero.");
            }

            var subCategory = new SubCategory
            {
                CategoryId = command.CategoryId,
                Name = command.Name,
                CommissionRate = command.CommissionRate
            };

            var newSubCategory = await _categoryRepository.AddSubCategoryAsync(subCategory);

            _context.AuditLogs.Add(new Audit
            {
                Action = $"SubCategory created: {newSubCategory.Name} (Rate: {newSubCategory.CommissionRate:P0}) under Category {parentCategory.Name}",
                PerformedBy = performedBy
            });
            await _context.SaveChangesAsync();
            return newSubCategory;
        }

        public async Task UpdateSubCategoryAsync(SubCategoryCommand command, string performedBy)
        {
            var subCategoryToUpdate = await _categoryRepository.GetSubCategoryByIdAsync(command.Id);
            if (subCategoryToUpdate == null)
            {
                throw new KeyNotFoundException($"SubCategory with ID {command.Id} not found.");
            }

            if (await _categoryRepository.SubCategoryNameExistsAsync(command.Name, command.CategoryId, command.Id))
            {
                throw new InvalidOperationException($"A subcategory named '{command.Name}' already exists in this category.");
            }

            subCategoryToUpdate.Name = command.Name;
            subCategoryToUpdate.CommissionRate = command.CommissionRate;

            await _categoryRepository.UpdateSubCategoryAsync(subCategoryToUpdate);

            _context.AuditLogs.Add(new Audit
            {
                Action = $"SubCategory updated: {subCategoryToUpdate.Name}",
                PerformedBy = performedBy
            });
            await _context.SaveChangesAsync();
        }
    }
}
