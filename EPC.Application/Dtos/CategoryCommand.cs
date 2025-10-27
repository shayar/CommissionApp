using System.ComponentModel.DataAnnotations;

namespace EPC.Application.Dtos
{
    public class CategoryCommand
    {
        // Used for Edit, null for Add
        public int Id { get; set; }

        [StringLength(50, ErrorMessage = "Category Code cannot exceed 50 characters.")]
        public string? CategoryId { get; set; }

        [Required(ErrorMessage = "Category Name is required.")]
        [StringLength(100, ErrorMessage = "Category Name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        // Commission rate for categories that do NOT have subcategories.
        // Nullable because it's only required if there are no subcategories (logic handled in service).
        [Range(0.00, 1.00, ErrorMessage = "Rate must be between 0.00 and 1.00 (e.g., 0.05 for 5%).")]
        public decimal? CommissionRate { get; set; }
    }
}
