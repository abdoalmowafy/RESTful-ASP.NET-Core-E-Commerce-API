using ECommerceAPI.Data;
using ECommerceAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceAPI.Controllers.Manage
{
    [Route("api/manage/PromoCode")]
    [ApiController]
    [Authorize(Roles = "Admin,Moderator")]
    public class ManagePromoCodeController(DataContext context) : ControllerBase
    {
        private readonly DataContext _context = context;

        [HttpGet]
        public async Task<IActionResult> IndexPromoCodes()
        {
            var usedInOrders = await _context.Orders.Include(o => o.PromoCode).Where(o => o.PromoCode != null)
                .GroupBy(o => o.PromoCode!.Id).ToDictionaryAsync(x => x.Key, y => y.Count());
            var usedInCarts = await _context.Carts.Include(c => c.PromoCode).Where(o => o.PromoCode != null)
                .GroupBy(c => c.PromoCode!.Id).ToDictionaryAsync(x => x.Key, y => y.Count());

            var promoCodes = await _context.PromoCodes.ToListAsync();
            var result = promoCodes.Select(promoCode => new
            {
                promoCode,
                UsedInOrders = usedInOrders.GetValueOrDefault(promoCode.Id),
                UsedInCarts = usedInCarts.GetValueOrDefault(promoCode.Id)
            });

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> NewPromoCode([FromBody] PromoCode promoCode)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _context.PromoCodes.Add(promoCode);
            await _context.SaveChangesAsync();

            return Ok(promoCode);
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdatePromoCodeStatus(int id)
        {
            var promo = await _context.PromoCodes.FindAsync(id);
            if (promo is null)
                return NotFound("Promo code not found!");

            promo.Active = !promo.Active;
            _context.PromoCodes.Update(promo);
            await _context.SaveChangesAsync(_context.Users.AsNoTracking().First(u => u.UserName == User.Identity!.Name));

            return Ok(promo);
        }
    }
}
