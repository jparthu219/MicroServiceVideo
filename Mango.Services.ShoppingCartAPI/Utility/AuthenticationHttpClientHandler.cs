using Microsoft.AspNetCore.Authentication;
using System.Net.Http.Headers;

namespace Mango.Services.ShoppingCartAPI.Utility
{
    public class AuthenticationHttpClientHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _accessor;

        public AuthenticationHttpClientHandler(IHttpContextAccessor accessor)
        {
            _accessor = accessor;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = _accessor.HttpContext != null 
                ? await _accessor.HttpContext.GetTokenAsync("access_token") 
                : null;

            if (string.IsNullOrEmpty(token))
            {
                var authHeader = _accessor.HttpContext?.Request.Headers["Authorization"].ToString();
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    token = authHeader.Substring("Bearer ".Length).Trim();
                }
            }

            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
