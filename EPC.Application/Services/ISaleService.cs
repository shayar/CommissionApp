using EPC.Application.Dtos;

namespace EPC.Application.Services
{
    public interface ISaleService
    {
        Task LogSaleAsync(string appUserId,
                          int categoryOrSubCategoryId,
                          decimal amount,
                          bool isSubCategory,
                          string paymentType,      // NEW
                          string? trackingNumber,   // NEW
                          string? description);     // NEW

        Task<SalesHistoryDto> GetEmployeeSalesHistoryAsync(string appUserId, SaleFilterDto filters);

        Task<SalesReportDto> GetAdminSalesReportAsync(AdminReportFilter filters);
        // Add existing methods like GetSalesByEmployeeAsync, etc. here later
    }
}
