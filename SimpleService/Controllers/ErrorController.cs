using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace SimpleService.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
public class ErrorController : ControllerBase
{
	[Route("/error")]
	public IActionResult HandleError()
	{
		var exceptionHandlerFeature = HttpContext.Features.Get<IExceptionHandlerFeature>();
		var exception = exceptionHandlerFeature?.Error;

		return Problem(
			title: "An error occurred while processing your request.",
			statusCode: StatusCodes.Status500InternalServerError
		);
	}
}
