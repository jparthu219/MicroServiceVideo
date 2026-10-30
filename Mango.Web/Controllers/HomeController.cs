using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Mango.Web.Models;
using Mango.Web.Service.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Mango.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IProductService _productService;
        private readonly ICartService _cartService;

        public HomeController(ILogger<HomeController> logger, IProductService productService, ICartService cartService)
        {
            _logger = logger;
            _productService = productService;
            _cartService = cartService;
        }

        public async Task<IActionResult> Index()
        {
            List<ProductDto>? list = new();
            ResponseDto? response = await _productService.GetAllProductsAsync();
            if (response != null && response.IsSuccess && response.Result != null)
            {
                list = JsonConvert.DeserializeObject<List<ProductDto>>(Convert.ToString(response.Result)!);
            }
            else
            {
                TempData["error"] = response?.Message;
            }
            return View(list);
        }

        public async Task<IActionResult> ProductDetails(int productId)
        {
            ProductDto? model = new();
            ResponseDto? response = await _productService.GetProductByIdAsync(productId);
            if (response != null && response.IsSuccess && response.Result != null)
            {
                model = JsonConvert.DeserializeObject<ProductDto>(Convert.ToString(response.Result)!);
            }
            else
            {
                TempData["error"] = response?.Message;
            }
            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ActionName("ProductDetails")]
        public async Task<IActionResult> ProductDetails(ProductDto productDto)
        {
            var userId = User.Claims.FirstOrDefault(u => u.Type == JwtRegisteredClaimNames.Sub)?.Value
                ?? User.Claims.FirstOrDefault(u => u.Type == ClaimTypes.NameIdentifier)?.Value
                ?? User.Identity?.Name ?? string.Empty;

            CartDto cartDto = new()
            {
                CartHeader = new CartHeaderDto
                {
                    UserId = userId
                },
                CartDetails = new List<CartDetailsDto>()
                {
                    new CartDetailsDto()
                    {
                        Count = productDto.Count,
                        ProductId = productDto.ProductId
                    }
                }
            };

            ResponseDto? response = await _cartService.UpsertCartAsync(cartDto);
            if (response != null && response.IsSuccess)
            {
                TempData["success"] = "Item has been added to the Shopping Cart";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                TempData["error"] = response?.Message;
            }

            return View(productDto);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
