using Dsw2026Tpi.Application.Dtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Application.Interfaces;
public interface IAvailabilityService
{
    Task<AvailabilityModel.Response>Create(AvailabilityModel.Request request);
    Task<AvailabilityModel.Response>Update(AvailabilityModel.Request request);
    Task<IReadOnlyCollection<AvailabilityModel.SlotResponse>>GetAvailable(Guid doctorId,DateOnly? date = null);
}
