using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Helpers;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;

namespace Dsw2026Tpi.Application.Services;

public class DoctorService : IDoctorService
{
    private readonly IPersistence _persistence;

    public DoctorService(IPersistence persistence)
    {
        _persistence = persistence;
    }

    public async Task<Pagination<DoctorModel.Response>> GetAll(int pageSize, int pageIndex, string? name = null)
    {

        ServiceValidation.ValidatePagination(pageSize, pageIndex); 
        ServiceValidation.ValidateOptionalName(name);

        String? filter = string.IsNullOrWhiteSpace(name) ? null : name.Trim();

        var doctors = await _persistence.Paginate<Doctor, string>(pageSize, pageIndex, doctor => filter == null 
        || doctor.Name.Contains(filter), doctor => doctor.Name,nameof(Doctor.Speciality));

        return doctors.Map(Map);
    }

    public async Task<DoctorModel.Response> Create(DoctorModel.Request request)
    {
        Validate(request);
        Speciality speciality = await GetSpeciality(request.SpecialityId);
        var doctor = new Doctor(
        request.Name!,
        request.LicenseNumber!,
        speciality);
        await _persistence.Add(doctor);
        await _persistence.SaveChanges();
        return Map(doctor);
    }

    public async Task<DoctorModel.Response> Update(Guid id, DoctorModel.Request request)
    {
        Validate(request);
        Speciality speciality = await GetSpeciality(request.SpecialityId);
        Doctor doctor = await _persistence.GetById<Doctor>(id, nameof(Doctor.Speciality)) ?? throw new EntityNotFoundException("Médico");
        doctor.Update(request.Name!, request.LicenseNumber!, speciality);
        await _persistence.Update(doctor);
        await _persistence.SaveChanges();
        return Map(doctor);
    }

    public async Task Delete(Guid id)
    {
        Doctor doctor = await _persistence.GetById<Doctor>(id) ?? throw new EntityNotFoundException( "Médico");
        await _persistence.Delete(doctor);
        await _persistence.SaveChanges();

    }

    public async Task<IReadOnlyCollection<AvailabilityModel.ScheduleResponse>> GetMonthlyAvailabilities(Guid doctorId)
    {
        _ = await _persistence.GetById<Doctor>(doctorId)
            ?? throw new EntityNotFoundException("Médico");

        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        DateOnly firstDay = new(today.Year, today.Month, 1);
        DateOnly lastDay = firstDay.AddMonths(1).AddDays(-1);

        var availabilities = (await _persistence.GetFiltered<Availability>(
                availability => availability.DoctorId == doctorId &&
                                availability.Date >= firstDay &&
                                availability.Date <= lastDay))
            .ToArray();

        return availabilities
            .GroupBy(x => x.Date.DayOfWeek)
            .OrderBy(x => DayOrder(x.Key))
            .Select(group => new AvailabilityModel.ScheduleResponse(
                DayName(group.Key),
                group.Min(x => x.StartTime).ToString("HH:mm"),
                group.Max(x => x.EndTime).ToString("HH:mm")))
            .ToArray();
    }
    private async Task<Speciality> GetSpeciality(Guid specialityId)
    {
        if (specialityId == Guid.Empty)
        {
            throw new EntityNotFoundException("Especialidad");
        }
        return await _persistence.GetById<Speciality>(specialityId) ?? throw new EntityNotFoundException("Especialidad");
    }
    private static void Validate(DoctorModel.Request request)
    {
        List<(string Field, string Issue)> errors = [];
        if (!request.Name.HasLengthBetween(3, 100))
        {
            errors.Add(("name", "La_longitud_debe_estar_entre_3_y_100"));
        }
        if (string.IsNullOrWhiteSpace(request.LicenseNumber))
        {
            errors.Add(("licenseNumber", "required"));
        }
        if (request.SpecialityId == Guid.Empty)
        {
            errors.Add(("specialityId","required"));
        }
        ServiceValidation.ThrowIfAny(errors);
    }
    private static DoctorModel.Response Map(Doctor doctor)
    {
        DoctorModel.SpecialityDto? speciality = doctor.Speciality is null ? null 
            : new DoctorModel.SpecialityDto(doctor.Speciality.Id, doctor.Speciality.Name);
        return new DoctorModel.Response(
                                          doctor.Id,
                                          doctor.Name,
                                          doctor.LicenseNumber,
                                          speciality
                                       );
    }

    private static string DayName(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => "LUNES",
        DayOfWeek.Tuesday => "MARTES",
        DayOfWeek.Wednesday => "MIÉRCOLES",
        DayOfWeek.Thursday => "JUEVES",
        DayOfWeek.Friday => "VIERNES",
        DayOfWeek.Saturday => "SÁBADO",
        DayOfWeek.Sunday => "DOMINGO",
        _ => day.ToString().ToUpperInvariant()
    };
    private static int DayOrder(DayOfWeek day) => day == DayOfWeek.Sunday ? 7 : (int)day;
}
