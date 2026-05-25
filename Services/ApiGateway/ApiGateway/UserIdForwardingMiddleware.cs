namespace ApiGateway
{
    using System.Security.Claims;

    public class UserIdForwardingMiddleware
    {
        private readonly RequestDelegate _next;

        public UserIdForwardingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!string.IsNullOrEmpty(userId))
                    context.Request.Headers["X-User-Id"] = userId;

                var role = context.User.FindFirst(ClaimTypes.Role)?.Value;
                if (!string.IsNullOrEmpty(role))
                    context.Request.Headers["X-User-Role"] = role;
            }

            await _next(context);
        }
    }
}
