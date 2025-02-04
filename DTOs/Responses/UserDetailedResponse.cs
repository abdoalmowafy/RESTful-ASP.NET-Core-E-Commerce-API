using ECommerceAPI.Models;

namespace ECommerceAPI.DTOs.Responses
{
    public class UserDetailedResponse
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public bool? EmailConfirmed { get; set; }
        public string? PhoneNumber { get; set; }
        public bool? PhoneNumberConfirmed { get; set; }
        public DateOnly? DOB { get; set; }
        public Gender? Gender { get; set; }
    }
}
