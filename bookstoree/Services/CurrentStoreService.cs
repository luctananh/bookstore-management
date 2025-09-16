using System.Security.Claims;

namespace bookstoree.Services
{
    public class CurrentStoreService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentStoreService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int? GetCurrentStoreId()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null && httpContext.User.Identity.IsAuthenticated)
            {
                var storeIdClaim = httpContext.User.FindFirst("StoreId");
                if (storeIdClaim != null && int.TryParse(storeIdClaim.Value, out int storeId))
                {
                    return storeId;
                }
            }
            return null;
        }
    }
}
