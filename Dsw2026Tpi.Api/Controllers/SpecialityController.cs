using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dsw2026Tpi.Api.Controllers

{
    [Authorize]
    [Route("api/specialties")]
    public class SpecialityController : AppController
    {
        private readonly ISpecialityService _service;
        public SpecialityController(ISpecialityService service)
        {
            _service = service;
        }
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
        [FromQuery] int pageSize = 10,
        [FromQuery] int pageIndex = 0,
        [FromQuery] string? name = null)
        {
            return Ok(await _service.GetAll(pageSize, pageIndex, name));

        }

[Authorize(Policy = Policies.AdminPolicy)]
        [HttpPost]
        [ProducesResponseType(typeof(SpecialityModel.Response), StatusCodes.Status201Created)]
        public async Task<IActionResult> Create([FromBody] SpecialityModel.Request request)
        {
            SpecialityModel.Response result = await _service.Create(request);
            return Created($"/api/specialties/{result.Id}"
            , result);
        }
        [Authorize(Policy = Policies.AdminPolicy)]
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(SpecialityModel.Response), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update(Guid id, [FromBody] SpecialityModel.Request
        request)
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

}

