namespace Mango.Services.ShoppingCartAPI.Models.Dto
{
    public class CartDto
    {
        public CartHeaderDto CartHeader { get; set; } = new();
        public IEnumerable<CartDetailsDto>? CartDetails { get; set; }
    }
}
