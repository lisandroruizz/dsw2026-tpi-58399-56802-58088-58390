using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dsw2026Tpi.Api.Controllers;

[Authorize]
[Route("api/availabilities")]
public class AvailabilityController : AppController
{
    private readonly IAvailabilityService _service;

    public AvailabilityController(IAvailabilityService service)
    {
        _service = service;
    }

    // Endpoint necesario para que el paciente obtenga los availabilityId reservables.
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailable(
        [FromQuery] Guid doctorId,
        [FromQuery] DateOnly? date = null)
    {
        return Ok(await _service.GetAvailable(doctorId, date));
    }

    [Authorize(Policy = Policies.AdminPolicy)]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] AvailabilityModel.Request request)
    {
        var result = await _service.Create(request);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [Authorize(Policy = Policies.AdminPolicy)]
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Update([FromBody] AvailabilityModel.Request request)
    {
        return Ok(await _service.Update(request));
    }
}
