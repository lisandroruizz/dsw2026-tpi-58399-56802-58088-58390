using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Helpers;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Enums;
using Dsw2026Tpi.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Dsw2026Tpi.Application.Services;

public class AppointmentService : IAppointmentService
{
    private const string AppointmentIncludes = "Availability.Doctor.Speciality";
    private readonly IPersistence _persistence;
    private readonly ILogger<AppointmentService> _logger;

    public AppointmentService(IPersistence persistence, ILogger<AppointmentService> logger)
    {
        _persistence = persistence;
        _logger = logger;
    }

    public async Task<AppointmentModel.Response> Create(
        AppointmentModel.Request request,
        long authenticatedDni)
    {
        ValidateCreate(request);

        if (request.Patient!.Dni != authenticatedDni)
        {
            throw new AuthorizationException();
        }

        Doctor doctor = await _persistence.GetById<Doctor>(request.DoctorId, nameof(Doctor.Speciality))
            ?? throw new EntityNotFoundException(ErrorCodeNames.DoctorNotFound, "Médico");

        Patient patient = await _persistence.First<Patient>(x => x.Dni == request.Patient.Dni)
            ?? throw new EntityNotFoundException(ErrorCodeNames.PatientNotFound, "Paciente");

        Availability availability = await _persistence.GetById<Availability>(
            request.AvailabilityId,
            "Doctor.Speciality")
            ?? throw new EntityNotFoundException(ErrorCodeNames.AvailabilityNotFound, "Disponibilidad");

        if (availability.DoctorId != doctor.Id)
        {
            throw new ValidationException(
            [
                ("availabilityId", "does_not_belong_to_doctor")
            ]);
        }

        if (availability.Status != AvailabilityStatus.Available ||
            availability.GetStartDateTime() <= DateTime.Now)
        {
            throw new ConflictException(
                ErrorCodeNames.AppointmentConflict,
                "El turno seleccionado no se encuentra disponible.")
                .WithDetail("availabilityId", "slot_unavailable");
        }

        availability.Reserve();

        var appointment = new Appointment(
            availability,
            patient,
            request.Reason!);

        await _persistence.Update(availability);
        await _persistence.Add(appointment);

        bool saved = await _persistence.TrySaveChanges();
        if (!saved) 
        {
            throw new ConflictException(
                ErrorCodeNames.AppointmentConflict,
                "El turno fue reservado por otro paciente.")
                .WithDetail("availabilityId", "slot_unavailable");
        }
         

        _logger.LogInformation(
            "El paciente {Dni} reservó la cita {AppointmentId} para el médico {DoctorId}",
            patient.Dni,
            appointment.Id,
            doctor.Id);

        return Map(appointment);
    }

    public async Task<IReadOnlyCollection<AppointmentModel.Response>> GetPatientActive(
        long dni,
        long authenticatedDni)
    {
        if (dni != authenticatedDni)
        {
            throw new AuthorizationException();
        }

        if (!dni.HasDigits(7, 10))
        {
            throw new ValidationException(
            [
                ("dni", "must_have_between_7_and_10_digits")
            ]);
        }

        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        TimeOnly currentTime = TimeOnly.FromDateTime(DateTime.Now);

        var appointments = await _persistence.GetFiltered<Appointment>(
            appointment => appointment.Patient.Dni == dni &&
                           appointment.Status == AppointmentStatus.Booked &&
                           (appointment.Availability.Date > today ||
                           (appointment.Availability.Date == today &&
                            appointment.Availability.StartTime > currentTime)),
            AppointmentIncludes,
            nameof(Appointment.Patient));

        return appointments
            .OrderBy(x => x.Availability.Date)
            .ThenBy(x => x.Availability.StartTime)
            .Select(Map)
            .ToArray();
    }

    public async Task Cancel(Guid id, long authenticatedDni)
    {
        Appointment appointment = await _persistence.GetById<Appointment>(
            id,
            nameof(Appointment.Availability),
            nameof(Appointment.Patient))
            ?? throw new EntityNotFoundException(ErrorCodeNames.AppointmentNotFound, "Cita");

        if (appointment.Patient.Dni != authenticatedDni)
        {
            throw new AuthorizationException();
        }

        if (appointment.Status != AppointmentStatus.Booked ||
            appointment.Availability.Status != AvailabilityStatus.Reserved)
        {
            throw new ConflictException(
                ErrorCodeNames.AppointmentInvalidState,
                "La cita y su disponibilidad no se encuentran en un estado cancelable.");
        }

        appointment.Cancel();
        appointment.Availability.Release();

        await _persistence.Update(appointment); 
        await _persistence.Update(appointment.Availability); 

        bool saved = await _persistence.TrySaveChanges();
        if (!saved) 
        { 
            throw new ConflictException(
                ErrorCodeNames.AppointmentConflict,
                "No fue posible cancelar la cita por un conflicto de concurrencia.");
        }
        

        _logger.LogInformation(
            "El paciente {Dni} canceló la cita {AppointmentId}",
            authenticatedDni,
            appointment.Id);
    }

    public async Task<IReadOnlyCollection<AppointmentModel.Response>> GetByDate(DateOnly date)
    {
        var appointments = await _persistence.GetFiltered<Appointment>(
            appointment => appointment.Availability.Date == date,
            AppointmentIncludes,
            nameof(Appointment.Patient));

        return appointments
            .OrderBy(x => x.Availability.StartTime)
            .Select(Map)
            .ToArray();
    }

    public async Task<Pagination<AppointmentModel.Response>> Search(
        int pageSize,
        int pageIndex,
        Guid? specialityId,
        Guid? doctorId,
        long? dni,
        DateOnly? date)
    {
        ServiceValidation.ValidatePagination(pageSize, pageIndex);

        if (dni.HasValue && !dni.Value.HasDigits(7, 10))
        {
            throw new ValidationException(
            [
                ("dni", "must_have_between_7_and_10_digits")
            ]);
        }

        var appointments = await _persistence.Paginate<Appointment, DateOnly>(
            pageSize,
            pageIndex,
            appointment =>
                (!specialityId.HasValue || appointment.Availability.Doctor.SpecialityId == specialityId.Value) &&
                (!doctorId.HasValue || appointment.Availability.DoctorId == doctorId.Value) &&
                (!dni.HasValue || appointment.Patient.Dni == dni.Value) &&
                (!date.HasValue || appointment.Availability.Date == date.Value),
            appointment => appointment.Availability.Date,
            AppointmentIncludes,
            nameof(Appointment.Patient));

        _logger.LogInformation(
            "Búsqueda avanzada de citas ejecutada. Especialidad: {SpecialityId}, Médico: {DoctorId}, DNI: {Dni}, Fecha: {Date}",
            specialityId,
            doctorId,
            dni,
            date);

        return appointments.Map(Map);
    }

    private static void ValidateCreate(AppointmentModel.Request request)
    {
        List<(string Field, string Issue)> errors = [];

        if (request.DoctorId == Guid.Empty)
        {
            errors.Add(("doctorId", "required"));
        }

        if (request.AvailabilityId == Guid.Empty)
        {
            errors.Add(("availabilityId", "required"));
        }

        if (request.Patient is null)
        {
            errors.Add(("patient", "required"));
        }
        else if (!request.Patient.Dni.HasDigits(7, 10))
        {
            errors.Add(("patient.dni", "must_have_between_7_and_10_digits"));
        }

        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length < 5)
        {
            errors.Add(("reason", "minimum_length_is_5"));
        }

        ServiceValidation.ThrowIfAny(errors);
    }

    private static AppointmentModel.Response Map(Appointment appointment)
    {
        Availability availability = appointment.Availability;
        Doctor doctor = availability.Doctor;
        string specialityName = doctor.Speciality?.Name ?? string.Empty;
        long dni = appointment.Patient?.Dni ?? 0;

        return new AppointmentModel.Response(
            appointment.Id,
            specialityName,
            doctor.Name,
            dni,
            availability.Date,
            availability.StartTime.ToString("HH:mm"),
            availability.EndTime.ToString("HH:mm"),
            StatusName(appointment.Status),
            appointment.Reason);
    }

    private static string StatusName(AppointmentStatus status) => status switch
    {
        AppointmentStatus.NoShow => "NO_SHOW",
        _ => status.ToString().ToUpperInvariant()
    };
}