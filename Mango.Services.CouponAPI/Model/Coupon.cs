using System.ComponentModel.DataAnnotations;

namespace Mango.Services.CouponAPI.Model
{
    public class Coupon
    {
        [Key]
        public int CouponId { get; set; }
        public string CouponCode { get; set; } = string.Empty;
        public double DiscountAmount { get; set; }
        public int minAmount { get; set; }
    }
}
