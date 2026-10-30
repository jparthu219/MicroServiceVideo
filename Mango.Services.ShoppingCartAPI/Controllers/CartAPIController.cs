using AutoMapper;
using Mango.Services.ShoppingCartAPI.Data;
using Mango.Services.ShoppingCartAPI.Models;
using Mango.Services.ShoppingCartAPI.Models.Dto;
using Mango.Services.ShoppingCartAPI.Service.IService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mango.Services.ShoppingCartAPI.Controllers
{
    [Route("api/cart")]
    [ApiController]
    public class CartAPIController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IMapper _mapper;
        private readonly IProductService _productService;
        private readonly ICouponService _couponService;
        private ResponseDto _response;

        public CartAPIController(
            AppDbContext db,
            IMapper mapper,
            IProductService productService,
            ICouponService couponService)
        {
            _db = db;
            _mapper = mapper;
            _productService = productService;
            _couponService = couponService;
            _response = new ResponseDto();
        }

        [HttpGet("GetCart/{userId}")]
        public async Task<ResponseDto> GetCart(string userId)
        {
            try
            {
                CartDto cart = new()
                {
                    CartHeader = _mapper.Map<CartHeaderDto>(_db.CartHeaders.First(u => u.UserId == userId))
                };

                cart.CartDetails = _mapper.Map<IEnumerable<CartDetailsDto>>(
                    _db.CartDetails.Where(u => u.CartHeaderId == cart.CartHeader.CartHeaderId));

                IEnumerable<ProductDto> productDtos = await _productService.GetProductsAsync();

                foreach (var item in cart.CartDetails)
                {
                    item.Product = productDtos.FirstOrDefault(u => u.ProductId == item.ProductId);
                    if (item.Product != null)
                    {
                        cart.CartHeader.CartTotal += (item.Count * item.Product.Price);
                    }
                }

                // Apply coupon logic if coupon exists
                if (!string.IsNullOrEmpty(cart.CartHeader.CouponCode))
                {
                    CouponDto? coupon = await _couponService.GetCouponAsync(cart.CartHeader.CouponCode);
                    if (coupon != null && cart.CartHeader.CartTotal > coupon.MinAmount)
                    {
                        cart.CartHeader.CartTotal -= coupon.DiscountAmount;
                        cart.CartHeader.Discount = coupon.DiscountAmount;
                    }
                }

                _response.Result = cart;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }

        [HttpPost("CartUpsert")]
        public async Task<ResponseDto> CartUpsert([FromBody] CartDto cartDto)
        {
            try
            {
                var cartHeaderFromDb = await _db.CartHeaders.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserId == cartDto.CartHeader.UserId);

                if (cartHeaderFromDb == null)
                {
                    // Create Cart Header
                    CartHeader cartHeader = _mapper.Map<CartHeader>(cartDto.CartHeader);
                    _db.CartHeaders.Add(cartHeader);
                    await _db.SaveChangesAsync();

                    // Create Cart Details
                    var firstDetail = cartDto.CartDetails!.First();
                    firstDetail.CartHeaderId = cartHeader.CartHeaderId;
                    _db.CartDetails.Add(_mapper.Map<CartDetails>(firstDetail));
                    await _db.SaveChangesAsync();
                }
                else
                {
                    // Check if Cart Details has the same product
                    var cartDetailsFromDb = await _db.CartDetails.AsNoTracking().FirstOrDefaultAsync(
                        u => u.ProductId == cartDto.CartDetails!.First().ProductId &&
                             u.CartHeaderId == cartHeaderFromDb.CartHeaderId);

                    if (cartDetailsFromDb == null)
                    {
                        // Create Cart Details
                        var firstDetail = cartDto.CartDetails!.First();
                        firstDetail.CartHeaderId = cartHeaderFromDb.CartHeaderId;
                        _db.CartDetails.Add(_mapper.Map<CartDetails>(firstDetail));
                        await _db.SaveChangesAsync();
                    }
                    else
                    {
                        // Update count in Cart Details
                        var firstDetail = cartDto.CartDetails!.First();
                        firstDetail.Count += cartDetailsFromDb.Count;
                        firstDetail.CartHeaderId = cartDetailsFromDb.CartHeaderId;
                        firstDetail.CartDetailsId = cartDetailsFromDb.CartDetailsId;
                        _db.CartDetails.Update(_mapper.Map<CartDetails>(firstDetail));
                        await _db.SaveChangesAsync();
                    }
                }

                _response.Result = cartDto;
            }
            catch (Exception ex)
            {
                _response.Message = ex.Message;
                _response.IsSuccess = false;
            }
            return _response;
        }

        [HttpPost("RemoveCart")]
        public async Task<ResponseDto> RemoveCart([FromBody] int cartDetailsId)
        {
            try
            {
                CartDetails cartDetails = _db.CartDetails.First(u => u.CartDetailsId == cartDetailsId);

                int totalCountofCartItem = _db.CartDetails.Where(u => u.CartHeaderId == cartDetails.CartHeaderId).Count();
                _db.CartDetails.Remove(cartDetails);

                if (totalCountofCartItem == 1)
                {
                    var cartHeaderToRemove = await _db.CartHeaders
                        .FirstOrDefaultAsync(u => u.CartHeaderId == cartDetails.CartHeaderId);

                    if (cartHeaderToRemove != null)
                    {
                        _db.CartHeaders.Remove(cartHeaderToRemove);
                    }
                }
                await _db.SaveChangesAsync();

                _response.Result = true;
            }
            catch (Exception ex)
            {
                _response.Message = ex.Message;
                _response.IsSuccess = false;
            }
            return _response;
        }

        [HttpPost("ApplyCoupon")]
        public async Task<ResponseDto> ApplyCoupon([FromBody] CartDto cartDto)
        {
            try
            {
                var cartFromDb = await _db.CartHeaders.FirstAsync(u => u.UserId == cartDto.CartHeader.UserId);
                cartFromDb.CouponCode = cartDto.CartHeader.CouponCode;
                _db.CartHeaders.Update(cartFromDb);
                await _db.SaveChangesAsync();
                _response.Result = true;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }

        [HttpPost("RemoveCoupon")]
        public async Task<ResponseDto> RemoveCoupon([FromBody] CartDto cartDto)
        {
            try
            {
                var cartFromDb = await _db.CartHeaders.FirstAsync(u => u.UserId == cartDto.CartHeader.UserId);
                cartFromDb.CouponCode = "";
                _db.CartHeaders.Update(cartFromDb);
                await _db.SaveChangesAsync();
                _response.Result = true;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }
    }
}
