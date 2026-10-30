using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Mango.Web.Models;
using Mango.Web.Service.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Mango.Web.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        [Authorize]
        public async Task<IActionResult> CartIndex()
        {
            return View(await LoadCartDtoBasedOnLoggedInUser());
        }

        public async Task<IActionResult> Remove(int cartDetailsId)
        {
            var response = await _cartService.RemoveFromCartAsync(cartDetailsId);
            if (response != null && response.IsSuccess)
            {
                TempData["success"] = "Cart updated successfully";
                return RedirectToAction(nameof(CartIndex));
            }
            TempData["error"] = response?.Message;
            return RedirectToAction(nameof(CartIndex));
        }

        [HttpPost]
        public async Task<IActionResult> ApplyCoupon(CartDto cartDto)
        {
            var response = await _cartService.ApplyCouponAsync(cartDto);
            if (response != null && response.IsSuccess)
            {
                TempData["success"] = "Coupon applied successfully";
                return RedirectToAction(nameof(CartIndex));
            }
            TempData["error"] = response?.Message;
            return RedirectToAction(nameof(CartIndex));
        }

        [HttpPost]
        public async Task<IActionResult> RemoveCoupon(CartDto cartDto)
        {
            cartDto.CartHeader.CouponCode = "";
            var response = await _cartService.ApplyCouponAsync(cartDto);
            if (response != null && response.IsSuccess)
            {
                TempData["success"] = "Coupon removed successfully";
                return RedirectToAction(nameof(CartIndex));
            }
            TempData["error"] = response?.Message;
            return RedirectToAction(nameof(CartIndex));
        }

        private async Task<CartDto> LoadCartDtoBasedOnLoggedInUser()
        {
            var userId = User.Claims.FirstOrDefault(u => u.Type == JwtRegisteredClaimNames.Sub)?.Value
                ?? User.Claims.FirstOrDefault(u => u.Type == ClaimTypes.NameIdentifier)?.Value
                ?? User.Identity?.Name ?? string.Empty;

            ResponseDto? response = await _cartService.GetCartByUserIdAsync(userId);
            if (response != null && response.IsSuccess && response.Result != null)
            {
                CartDto cartDto = JsonConvert.DeserializeObject<CartDto>(Convert.ToString(response.Result)!) ?? new CartDto();
                return cartDto;
            }

            return new CartDto();
        }
    }
}
