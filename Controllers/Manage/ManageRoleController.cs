using ECommerceAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using ECommerceAPI.Data;
using Microsoft.AspNetCore.Identity;
using ECommerceAPI.DTOs.Responses;
using Microsoft.EntityFrameworkCore;

namespace ECommerceAPI.Controllers.Manage
{
    [Route("api/[action]")]
    [ApiController]
    [Authorize(Roles = "Admin,Moderator")]
    public class ManageRoleController(DataContext context, UserManager<User> userManager, RoleManager<IdentityRole> roleManager) : ControllerBase
    {
        private readonly DataContext _context = context;
        private readonly UserManager<User> _userManager = userManager;
        private readonly RoleManager<IdentityRole> _roleManager = roleManager;

        [HttpGet]
        public IActionResult IndexRoles()
        {
            return Ok(_roleManager.Roles.AsNoTracking());
        }

        [HttpGet]
        public async Task<IActionResult> IndexUsers(string role = "", string targetUser = "", int pageIndex = 1)
        {
            var users = _context.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(targetUser)) 
                users = users.Where(u => u.UserName!.Contains(targetUser.Trim()));

            var userRoles = new List<dynamic>();

            if(!string.IsNullOrWhiteSpace(role) && await _roleManager.RoleExistsAsync(role.Trim()))
                return NotFound($"Role '{role}' is missing!");

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                bool hasRole = string.IsNullOrWhiteSpace(role) || roles.Contains(role);

                if (hasRole) userRoles.Add(new { user.Id, user.Name, user.Email, roles });
            }

            return Ok(new { userRoles = userRoles.ToPaginatedList(pageIndex, 20), role, targetUser });
        }

        [HttpPatch]
        public async Task<IActionResult> UpdateRole(string userId, string roleName)
        {
            if (roleName == "Admin") return Forbid();

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null) return NotFound($"User with ID '{userId}' not found.");

            if (!await _roleManager.RoleExistsAsync(roleName)) return BadRequest($"Role '{roleName}' does not exist.");

            var isInRole = await _userManager.IsInRoleAsync(user, roleName);
            IdentityResult result;

            if (isInRole)
            {
                result = await _userManager.RemoveFromRoleAsync(user, roleName);
                if (result.Succeeded) return Ok($"'{roleName}' role removed from user '{user.UserName}' successfully.");
            }
            else
            {
                result = await _userManager.AddToRoleAsync(user, roleName);
                if (result.Succeeded) return Ok($"'{roleName}' role added to user '{user.UserName}' successfully.");
            }

            return BadRequest(result.Errors);
        }
    }
}

