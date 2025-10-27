using EPC.Application.Dtos;

namespace EPC.Application.Services
{
    public interface IDashboardService
    {
        // Employee dashboard view
        Task<EmployeeSummaryDto> GetEmployeeSummaryAsync(string userId, DateTime startDate);

        // Admin dashboard views
        Task<List<EmployeePerformanceDto>> GetTopEmployeePerformanceAsync(DateTime startDate, int count = 5);
        Task<List<CategorySalesDto>> GetSalesByCategoryAsync(DateTime startDate);
    }
}
