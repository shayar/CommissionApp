namespace EPC.WEB.Middleware
{
    public class AppLockMiddleware
    {
        private readonly RequestDelegate _next;
        public AppLockMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            if (File.Exists("app_locked.txt") && !context.User.IsInRole("Developer"))
            {
                context.Response.Redirect("/Locked");
                return;
            }
            await _next(context);
        }
    }
    public static class AppLockMiddlewareExtensions
    {
        public static IApplicationBuilder UseAppLock(this IApplicationBuilder builder)
            => builder.UseMiddleware<AppLockMiddleware>();
    }
}
