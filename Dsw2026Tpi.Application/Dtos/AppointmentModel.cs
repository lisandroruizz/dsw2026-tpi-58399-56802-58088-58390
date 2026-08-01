using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Application.Dtos;

public record AppointmentModel {
    public record Request(
        Guid DoctorId,
        Guid AvailabilityId,
        PatientRequest? Patient, 
        string? Reason); 
    public record PatientRequest(long Dni);
    public record Response(
        Guid Id,
        string Specialty,
        string Doctor, 
        long Dni,
        DateOnly Date,
        string StartTime,
        string EndTime, 
        string Status,
        string Reason);
}
