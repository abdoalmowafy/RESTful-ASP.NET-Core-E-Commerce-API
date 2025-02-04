using ECommerceAPI.Data;
using ECommerceAPI.DTOs.Requests;
using ECommerceAPI.Models;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceAPI.Controllers.Manage
{
    [Route("api/manage/[action]")]
    [ApiController]
    [Authorize(Roles = "Admin,Moderator")]
    public class ManageStoreAddressController(DataContext context) : ControllerBase
    {
        private readonly DataContext _context = context;

        [HttpGet]
        public async Task<IActionResult> IndexStoreAddresses()
        {
            var storeAddresses = await _context.StoreAddresses.AsNoTracking().ToListAsync();
            return Ok(storeAddresses);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> IndexStoreAddress(int id)
        {
            var storeAddress = await _context.StoreAddresses.AsNoTracking().FirstOrDefaultAsync(sa => sa.Id == id);

            if (storeAddress is null) return NotFound("Store address not found!");

            return Ok(storeAddress);
        }

        [HttpPost]
        public async Task<IActionResult> NewStoreAddress([FromBody] AddressRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var storeAddress = request.Adapt<Address>();

            _context.StoreAddresses.Add(storeAddress);
            await _context.SaveChangesAsync();

            return Ok(storeAddress);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> EditStoreAddress(int id, [FromBody] AddressRequest request)
        {
            var storeAddress = await _context.StoreAddresses.FindAsync(id);

            if (storeAddress is null) return NotFound("Store address not found!");

            if (!ModelState.IsValid) return BadRequest(ModelState);

            request.Adapt(storeAddress);

            _context.StoreAddresses.Update(storeAddress);
            await _context.SaveChangesAsync(_context.Users.AsNoTracking().First(u => u.UserName == User.Identity!.Name));

            return Ok(storeAddress);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStoreAddress(int id)
        {
            var storeAddress = await _context.StoreAddresses.FindAsync(id);

            if (storeAddress is null) return NotFound("Store address not found!");

            storeAddress.DeletedDateTime = DateTime.Now;
            _context.DeletesHistory.Add(new DeleteHistory
            {
                Deleter = await _context.Users.FirstAsync(u => u.UserName == User.Identity!.Name),
                DeletedType = nameof(Address),
                DeletedId = id
            });
            await _context.SaveChangesAsync();

            return Ok("Address was deleted successfully!");
        }
    }
}
