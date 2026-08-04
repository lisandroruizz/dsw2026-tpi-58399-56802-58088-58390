using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Dsw2026Tpi.Application.Dtos;

public record AppointmentModel {
    public record Request(
        Guid DoctorId,
        [property:JsonPropertyName("availabilitySlotId")]
        Guid AvailabilitySlotId,
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

    public record AdminResponse(
       Guid AppointmentsId,
       string AppointmentsStatus,
       PatientSummary Patient,
       DoctorSummary Doctor);

    public record PatientSummary(
        long Dni,
        string FullName);

    public record DoctorSummary(
        Guid DoctorId,
        string Name,
        SpecialtySummary Specialty);

    public record SpecialtySummary(
        Guid SpecialtyId,
        string Name);
}
