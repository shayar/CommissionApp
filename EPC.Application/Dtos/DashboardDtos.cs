namespace EPC.Application.Dtos
{
    /// <summary>
    /// DTO for displaying a single employee's monthly/period summary.
    /// </summary>
    public class EmployeeSummaryDto
    {
        public decimal TotalSales { get; set; }
        public decimal TotalCommission { get; set; }
        public int SaleCount { get; set; }
    }

    /// <summary>
    /// DTO for displaying aggregated performance rankings (Admin View).
    /// </summary>
    public class EmployeePerformanceDto
    {
        public string AppUserId { get; set; } = string.Empty;

        // This is a placeholder; the Presentation Layer (Razor Page Model) 
        // will fetch the FullName using the UserManager before display.
        public string EmployeeFullName { get; set; } = "N/A";

        public decimal TotalSales { get; set; }
        public decimal TotalCommission { get; set; }
    }

    /// <summary>
    /// DTO for sales totals grouped by category (Admin Chart View).
    /// </summary>
    public class CategorySalesDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal TotalSales { get; set; }
    }
}
