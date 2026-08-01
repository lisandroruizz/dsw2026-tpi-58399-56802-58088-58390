using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dsw2026Tpi.Api.Controllers;

[Authorize]
[Route("api/appointments")]
public class AppointmentController : AppController
{
    private readonly IAppointmentService _service;

    public AppointmentController(IAppointmentService service)
    {
        _service = service;
    }

    [Authorize(Policy = Policies.PatientPolicy)]
    [HttpPost]
    [ProducesResponseType(typeof(AppointmentModel.Response), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] AppointmentModel.Request request)
    {
        long dni = GetAuthenticatedPatientDni();
        AppointmentModel.Response result = await _service.Create(request, dni);
        return Created($"/api/appointments/{result.Id}", result);
    }

    [Authorize(Policy = Policies.PatientPolicy)]
    [HttpGet("patient")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPatientAppointments([FromQuery] long dni)
    {
        return Ok(await _service.GetPatientActive(dni, GetAuthenticatedPatientDni()));
    }

    [Authorize(Policy = Policies.PatientPolicy)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Cancel(Guid id)
    {
        await _service.Cancel(id, GetAuthenticatedPatientDni());
        return NoContent();
    }

    [Authorize(Policy = Policies.AdminPolicy)]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByDate([FromQuery] DateOnly date)
    {
        return Ok(await _service.GetByDate(date));
    }

    [Authorize(Policy = Policies.AdminPolicy)]
    [HttpGet("search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] int pageSize = 10,
        [FromQuery] int pageIndex = 0,
        [FromQuery] Guid? specialtyId = null,
        [FromQuery] Guid? doctorId = null,
        [FromQuery] long? dni = null,
        [FromQuery] DateOnly? date = null)
    {
        return Ok(await _service.Search(
            pageSize,
            pageIndex,
            specialtyId,
            doctorId,
            dni,
            date));
    }
}