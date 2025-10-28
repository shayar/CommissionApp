namespace EPC.Application.Dtos
{
    public class ReportSaleDetailDto
    {
        public DateTime Date { get; set; }
        public string EmployeeName { get; set; } = default!;
        public string CategoryName { get; set; } = default!;
        public string SubCategoryName { get; set; } = default!;
        public decimal Amount { get; set; }
        public decimal CommissionEarned { get; set; }
        public string PaymentType { get; set; } = default!;
        public string? TrackingNumber { get; set; }
        public string? Description { get; set; }
    }
    public class SalesReportDto
    {
        public List<ReportSaleDetailDto> SalesDetails { get; set; } = new();
        public Dictionary<string, decimal> TotalsByCategory { get; set; } = new();
        public decimal GrandTotalSales { get; set; }
        public decimal GrandTotalCommission { get; set; }
    }

    public class AdminReportFilter
    {
        public string? AppUserId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? SearchTerm { get; set; }
    }

}
