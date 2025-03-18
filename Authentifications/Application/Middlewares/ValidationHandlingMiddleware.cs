using System.Security.Authentication;
using Authentifications.Core.Entities;
namespace Authentifications.Application.Middlewares;
public class ValidationHandlingMiddleware
{
	private readonly RequestDelegate _next;
	public ValidationHandlingMiddleware(RequestDelegate next)
	{
		_next = next;
	}
	public async Task InvokeAsync(HttpContext context)
	{
		try
		{
			await _next(context);
		}
		catch (Exception ex)
		{
			await HandleExceptionAsync(context, ex);
		}
		// Vérifier si le modèle est invalide et gérer manuellement l'erreur 400
		if (context.Items.ContainsKey("ModelValidationErrors") && !context.Response.HasStarted)
		{
			var validationErrors = (List<string>)context.Items["ModelValidationErrors"]!;
			context.Response.StatusCode = 400;
			context.Response.ContentType = "application/json";
			var response = new
			{
				Type = "Error : ValidationError for DataModel",
				Title = "You must enter requested values for all fields.",
				Status = 400,
				Errors = validationErrors
			};
			await context.Response.WriteAsJsonAsync(response);
		}
	}
	private async Task HandleExceptionAsync(HttpContext context, Exception exception)
	{
		context.Response.ContentType = "application/json";
		context.Response.StatusCode = StatusCodes.Status500InternalServerError;
		var response = new Message
		{
			TraceId = context.TraceIdentifier,
			Type = "ServerError",
			Title = "An unexpected error occurred.",
			Status = StatusCodes.Status500InternalServerError
		};
		switch (exception)
		{
			case AuthentificationBasicException:
				context.Response.StatusCode = StatusCodes.Status403Forbidden;
				response.Type = "AuthenticationError";
				response.Title = "Request has been authenticated successfully.";
				response.Detail = "However, it is not eligible for JWT authentication.";
				response.Status = StatusCodes.Status403Forbidden;
				response.TraceId = context.TraceIdentifier;
				break;

			case AuthenticationException:
				context.Response.StatusCode = StatusCodes.Status401Unauthorized;
				response.Type = "Unauthorized";
				response.Title = "Invalid credentials.";
				response.Status = StatusCodes.Status401Unauthorized;
				response.TraceId = context.TraceIdentifier;
				break;

			case KeyNotFoundException:
				context.Response.StatusCode = StatusCodes.Status404NotFound;
				response.Type = "NotFound";
				response.Title = "The requested resource was not found.";
				response.Status = StatusCodes.Status404NotFound;
				break;

			case ArgumentException:
				context.Response.StatusCode = StatusCodes.Status400BadRequest;
				response.Type = "BadRequest";
				response.Title = "Invalid argument provided.";
				response.Status = StatusCodes.Status400BadRequest;
				break;
			default:
				break;
		}
		await context.Response.WriteAsJsonAsync(response);
	}
}