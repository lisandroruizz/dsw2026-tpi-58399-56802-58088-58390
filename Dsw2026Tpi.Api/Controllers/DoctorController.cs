using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Dsw2026Tpi.Application.Dtos;


namespace Dsw2026Tpi.Api.Controllers;

[Route("api/doctors")]
[Authorize(Policy = Policies.AdminPolicy)]
public class DoctorController : AppController
{
    private readonly IDoctorService _service;

    public DoctorController(IDoctorService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int pageSize = 10, [FromQuery] int pageIndex = 0, [FromQuery] string? name = null)
    {
        return Ok(await _service.GetAll(pageSize, pageIndex, name));

    }

    [HttpGet("{id:guid}/availabilities")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailabilities(Guid id)
    {
        return Ok(await _service.GetMonthlyAvailabilities(id));
    }   

    [Authorize(Policy = Policies.AdminPolicy)]
        [HttpPost]
        [ProducesResponseType(typeof(DoctorModel.Response), StatusCodes.Status201Created)]

        public async Task<IActionResult> Create([FromBody] DoctorModel.Request request)
        {
            DoctorModel.Response result = await _service.Create(request);
            return Created($"/api/doctors/{result.Id}", result);
        }
        [Authorize(Policy = Policies.AdminPolicy)]
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(DoctorModel.Response), StatusCodes.Status200OK)]

        public async Task<IActionResult> Update(Guid id, [FromBody] DoctorModel.Request request)
        {
            return Ok(await _service.Update(id, request));
        }
        [Authorize(Policy = Policies.AdminPolicy)]
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]

        public async Task<IActionResult> Delete(Guid id)
        {
            await _service.Delete(id);
            return NoContent();
        }
    
}

