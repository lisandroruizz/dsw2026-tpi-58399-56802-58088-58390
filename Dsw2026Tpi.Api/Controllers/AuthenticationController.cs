using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Dsw2026Tpi.CrossCutting.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Dsw2026Tpi.Api.Resources;

namespace Dsw2026Tpi.Api.Controllers;

[Route("api/auth")]
public class AuthenticationController : AppController
{
    private readonly IAuthenticationService _authenticationService;

    public AuthenticationController(IAuthenticationService authenticationService) 
    {
        _authenticationService = authenticationService;
    }


    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.AdminLogin)]
    [HttpPost("admin/login")]
    [ProducesResponseType(typeof(LoginAdminModel.Response), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]

    public async Task<IActionResult> LoginAdmin([FromBody] LoginAdminModel.Request request)
    {
       
        return Ok(await _authenticationService.LoginAdmin(request)); 
    }



    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.PatientLogin)]
    [HttpPost("patient/login")]
    [ProducesResponseType(typeof(LoginPatientModel.Response), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> LoginPatient([FromBody] LoginPatientModel.Request request)
    { 
        return Ok(await _authenticationService.LoginPatient(request));
    }

    [Authorize(Policy = Policies.AdminPolicy)]
    [HttpPost("admin/register")]
    [ProducesResponseType(typeof(RegisterModel.Response), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]

    public async Task<IActionResult> Register([FromBody] RegisterModel.Request request)
    {
        RegisterModel.Response result = await _authenticationService.Register(request);
        return StatusCode(StatusCodes.Status201Created, result); 
    }

}
