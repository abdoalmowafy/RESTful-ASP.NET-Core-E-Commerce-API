using ECommerceAPI.Data;
using ECommerceAPI.DTOs.Responses;
using ECommerceAPI.Models;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceAPI.Controllers
{
    [Route("api/[action]")]
    [ApiController]
    [Authorize]
    public class CartController(DataContext context) : ControllerBase
    {
        private readonly DataContext _context = context;

        [HttpGet]
        public async Task<IActionResult> IndexCart()
        {
            var user = await _context.Users
                .Include(u => u.Addresses)
                .Include(u => u.Cart)
                    .ThenInclude(c => c.CartProducts)
                        .ThenInclude(cp => cp.Product)
                .Include(u => u.Cart)
                    .ThenInclude(c => c.PromoCode)
                .AsNoTracking()
                .FirstAsync(u => u.UserName == User.Identity!.Name);

            var cart = user.Cart!;

            var invalidCartProducts = cart.CartProducts.Where(cp => cp.Product.DeletedDateTime.HasValue || cp.Product.Quantity < 1 || cp.Quantity > cp.Product.Quantity).ToList();
            if (invalidCartProducts.Count != 0)
            {
                foreach (var cartProduct in invalidCartProducts)
                {
                    cart.CartProducts.Remove(cartProduct);
                    _context.CartProducts.Remove(cartProduct);
                }
                _context.Carts.Update(cart);
                await _context.SaveChangesAsync();
            }

            if (cart.PromoCode is not null && !cart.PromoCode.Active)
            {
                cart.PromoCode = null;
                _context.Carts.Update(cart);
                await _context.SaveChangesAsync();
            }

            return Ok(cart.Adapt<CartResponse>());
        }

        [HttpPatch]
        public async Task<IActionResult> UpdateCartProducts(int productId, int count = 1)
        {
            var product = await _context.Products.FindAsync(productId);

            if (product is null || product.DeletedDateTime.HasValue || product.Quantity < 1 || count > product.Quantity)
                return NotFound("Product not found or invalid quantity!");

            var user = await _context.Users
                .Include(u => u.Cart)
                    .ThenInclude(c => c.CartProducts)
                .FirstAsync(u => u.UserName == User.Identity!.Name);

            var cart = user.Cart!;

            var cartProduct = cart.CartProducts.FirstOrDefault(x => x.Product == product);

            if (count > 0)
            {
                if (cartProduct is null)
                {
                    cartProduct = new CartProduct
                    {
                        Product = product,
                        Quantity = count
                    };
                    _context.CartProducts.Add(cartProduct);
                    cart.CartProducts.Add(cartProduct);
                    _context.Carts.Update(cart);
                }
                else
                {
                    cartProduct.Quantity = count;
                    _context.CartProducts.Update(cartProduct);
                }
            }
            else
            {
                if (cartProduct is not null)
                {
                    cart.CartProducts.Remove(cartProduct);
                    _context.CartProducts.Remove(cartProduct);
                    _context.Carts.Update(cart);
                }
            }
            await _context.SaveChangesAsync();
            return Ok(cart.Adapt<CartResponse>());
        }

        [HttpPatch]
        public async Task<IActionResult> ApplyPromoCode(string? promoCode)
        {
            var user = await _context.Users
                .Include(u => u.Cart)
                    .ThenInclude(c => c.PromoCode)
                .Include(u => u.Cart)
                    .ThenInclude(c => c.CartProducts)
                        .ThenInclude(cp => cp.Product)
                .FirstAsync(u => u.UserName == User.Identity!.Name);

            var cart = user.Cart!;

            if (string.IsNullOrWhiteSpace(promoCode))
            {
                cart.PromoCode = null;
                _context.Carts.Update(cart);
                await _context.SaveChangesAsync();
            }
            else
            {
                var promo = await _context.PromoCodes.FirstOrDefaultAsync(x => x.Code == promoCode);
                if (promo is null)
                {
                    return NotFound("Promo code not found!");
                }
                else
                {
                    cart.PromoCode = promo;
                    _context.Carts.Update(cart);
                    await _context.SaveChangesAsync();
                }
            }
            return Ok(cart.Adapt<CartResponse>());
        }
    }
}
