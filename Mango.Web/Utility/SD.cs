namespace Mango.Web.Utility
{
    public class SD
    {
        public static string CouponAPIBase { get; set; } = string.Empty;
        public static string ProductAPIBase { get; set; } = string.Empty;
        public static string AuthAPIBase { get; set; } = string.Empty;
        public static string ShoppingCartAPIBase { get; set; } = string.Empty;
        public const string RoleAdmin = "ADMIN";
        public const string RoleCustomer = "CUSTOMER";
        public const string TokenCookie = "JWTToken";
        public enum ApiType
        {
            GET,
            POST,
            PUT,
            DELETE
        }
    }
}
