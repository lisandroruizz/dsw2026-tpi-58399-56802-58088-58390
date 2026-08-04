using System;
using System.Collections.Generic;
using System.Text;
using Dsw2026Tpi.Application.Dtos; 
using Dsw2026Tpi.Domain.Entities; 

namespace Dsw2026Tpi.Application.Interfaces;

public interface IAppointmentService
{ 
    Task<AppointmentModel.Response> Create(AppointmentModel.Request request, long authenticatedDni);
    Task<IReadOnlyCollection<AppointmentModel.Response>> GetPatientActive(long dni, long authenticatedDni);
    Task Cancel(Guid id, long authenticatedDni);

    Task<Pagination<AppointmentModel.AdminResponse>>GetByDate(
           int pageSize,
           int pageIndex,
           DateOnly date);
    Task<Pagination<AppointmentModel.AdminResponse>> Search(
        int pageSize,
        int pageIndex,
        Guid? specialityId, 
        Guid? doctorId, 
        long? dni,
        DateOnly? date);
}