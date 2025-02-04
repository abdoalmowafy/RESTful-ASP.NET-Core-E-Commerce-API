using ECommerceAPI.Models;
using System.ComponentModel.DataAnnotations;

namespace ECommerceAPI.DTOs.Requests
{
    public class UserUpdateRequest
    {
        public string? Name { get; set; }
        public DateOnly? DOB { get; set; }
        public Gender? Gender { get; set; }
        [DataType(DataType.Password)] public string? NewPassword { get; set; }
        [DataType(DataType.Password)] public required string OldPassword { get; set; }
    }
}
