using Microsoft.AspNetCore.Identity;
using Piece.Data;

namespace Piece.Middleware
{
	public class BanCheckMiddleware
	{
		private readonly RequestDelegate _next;

		public BanCheckMiddleware(RequestDelegate next)
		{
			_next = next;
		}

		public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager)
		{
			var path = context.Request.Path.Value?.ToLower() ?? "";

			if (path.Contains("/account/banned") ||
				path.Contains("/account/logout") ||
				path.Contains("/account/login") ||
				path.Contains("/account/register") ||
				path.StartsWith("/_framework") ||
				path.StartsWith("/_content") ||
				path.StartsWith("/css") ||
				path.StartsWith("/js") ||
				path.Contains(".css") ||
				path.Contains(".js") ||
				path.Contains(".png") ||
				path.Contains(".jpg") ||
				path.Contains(".ico") ||
				path.Contains(".woff") ||
				path.Contains("/music/") ||
				path.Contains("/images/"))
			{
				await _next(context);
				return;
			}

			if (context.User.Identity?.IsAuthenticated == true)
			{
				var userId = userManager.GetUserId(context.User);
				if (!string.IsNullOrEmpty(userId))
				{
					var user = await userManager.FindByIdAsync(userId);
					if (user?.IsBanned == true && !path.Contains("/account/banned"))
					{
						context.Response.Redirect("/Account/Banned");
						return;
					}
				}
			}

			await _next(context);
		}
	}
}