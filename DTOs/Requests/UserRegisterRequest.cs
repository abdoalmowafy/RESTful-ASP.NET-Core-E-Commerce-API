using ECommerceAPI.Models;
using System.ComponentModel.DataAnnotations;

namespace ECommerceAPI.DTOs.Requests
{
    public class UserRegisterRequest
    {
        [Required] public required string Name { get; set; }
        [Required][DataType(DataType.EmailAddress)] public required string Email { get; set; }
        [Required][DataType(DataType.Password)] public required string Password { get; set; }
        public DateOnly? DOB { get; set; }
        public Gender? Gender { get; set; }
    }
}
