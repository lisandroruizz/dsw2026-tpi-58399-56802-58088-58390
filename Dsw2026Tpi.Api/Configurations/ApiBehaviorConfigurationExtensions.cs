using Dsw2026Tpi.CrossCutting.Models;
using Dsw2026Tpi.CrossCutting.Resources;
using Microsoft.AspNetCore.Mvc;

namespace Dsw2026Tpi.Api.Configurations;

public static class ApiBehaviorConfigurationExtensions
{
    public static IMvcBuilder AddAppControllers(this IServiceCollection services)
    {
        return services
            .AddControllers()
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var response = new ErrorResponse(
                        nameof(ErrorCodes.VALIDATION_ERROR),
                        ErrorCodes.VALIDATION_ERROR);

                    foreach (var entry in context.ModelState.Where(entry =>
                        entry.Value?.Errors.Count > 0))
                    {
                        foreach (var error in entry.Value!.Errors)
                        {
                            response.AddDetail(
                                entry.Key,
                                string.IsNullOrWhiteSpace(error.ErrorMessage)
                                    ? "invalid_value"
                                    : error.ErrorMessage);
                        }
                    }

                    return new BadRequestObjectResult(response);
                };
            });
    }
}