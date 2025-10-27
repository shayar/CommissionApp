using EPC.Application.Dtos;
using EPC.Domain.Entities;

namespace EPC.Application.Services
{
    public interface ICategoryManagementService
    {
        // Fetch
        Task<List<Category>> GetAllCategoriesAsync();

        // Category CRUD
        Task<Category> AddCategoryAsync(CategoryCommand command, string performedBy);
        Task UpdateCategoryAsync(CategoryCommand command, string performedBy);

        // SubCategory CRUD
        Task<SubCategory> AddSubCategoryAsync(SubCategoryCommand command, string performedBy);
        Task UpdateSubCategoryAsync(SubCategoryCommand command, string performedBy);
    }
}
