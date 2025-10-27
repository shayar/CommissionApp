namespace EPC.Application.Dtos
{
    public class UserPasswordResetDtos
    {
        public string Id { get; set; } = default!;
        public string? Email { get; set; }
        public string? StoreId { get; set; }
        public string? EmployeeId { get; set; }
        public bool IsLockedOut { get; set; }
        public string LockoutStatusDisplay { get; set; } = "No";
    }
}
