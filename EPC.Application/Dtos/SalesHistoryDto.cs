namespace EPC.Application.Dtos
{
    public class SaleItemDto
    {
        public DateTime Date { get; set; }
        public string CategoryName { get; set; } = default!;
        public string SubCategoryName { get; set; } = default!;
        public decimal Amount { get; set; }
        public decimal CommissionEarned { get; set; }
        public string PaymentType { get; set; } = default!;
        public string? Description { get; set; }
        public string? TrackingNumber { get; set; }
    }

    public class SalesHistoryDto
    {
        public List<SaleItemDto> Sales { get; set; } = new List<SaleItemDto>();
        public decimal TotalSalesAmount { get; set; }
        public decimal TotalCommissionAmount { get; set; }
    }

    public class SaleFilterDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? CategoryId { get; set; }
        public int? SubCategoryId { get; set; }
    }
}
