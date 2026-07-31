using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Enums;
using Dsw2026Tpi.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Dsw2026Tpi.Application.Services
{
    public class AvailabilityService : IAvailabilityService
    {
        private readonly IPersistence _persistence;
        private readonly ILogger<AvailabilityService> _logger;
        private const string CodigoSuperposicionDisponibilidad = "AVAILABILITY_OVERLAP";

        public AvailabilityService(IPersistence persistence, ILogger<AvailabilityService> logger)
        {
            _persistence = persistence;
            _logger = logger;
        }

        public Task<IReadOnlyCollection<AvailabilityModel.SlotResponse>> Create(AvailabilityModel.Request request)
        {
            return GuardarCronogramaMensual(request, overwrite: false);
        }

        public Task<IReadOnlyCollection<AvailabilityModel.SlotResponse>> Update(AvailabilityModel.Request request)
        {
            return GuardarCronogramaMensual(request, overwrite: true);
        }

        public async Task<IReadOnlyCollection<AvailabilityModel.SlotResponse>> GetAvailable(
            Guid doctorId,
            DateOnly? date = null)
        {
            _ = await _persistence.GetById<Doctor>(doctorId)
                ?? throw new EntityNotFoundException("Médico");

            DateOnly hoy = DateOnly.FromDateTime(DateTime.Today);
            DateOnly ultimoDia = new DateOnly(hoy.Year, hoy.Month, 1).AddMonths(1).AddDays(-1);
            DateOnly desde = date ?? hoy;
            DateOnly hasta = date ?? ultimoDia;
            DateTime ahora = DateTime.Now;

            var disponibilidades = await _persistence.GetFiltered<Availability>(
                availability => availability.DoctorId == doctorId &&
                                availability.Date >= desde &&
                                availability.Date <= hasta &&
                                availability.Status == AvailabilityStatus.Available);

            return disponibilidades
                .Where(disponibilidad => disponibilidad.GetStartDateTime() > ahora)
                .OrderBy(disponibilidad => disponibilidad.Date)
                .ThenBy(disponibilidad => disponibilidad.StartTime)
                .Select(Map)
                .ToArray();
        }

        private async Task<IReadOnlyCollection<AvailabilityModel.SlotResponse>> GuardarCronogramaMensual(
            AvailabilityModel.Request request,
            bool overwrite)
        {
            Doctor doctor = await _persistence.GetById<Doctor>(request.DoctorId)
                ?? throw new EntityNotFoundException("Médico");

            IReadOnlyCollection<ParsedDay> days = InterpretarYValidarDias(request.Days);

            DateTime ahora = DateTime.Now;
            DateOnly hoy = DateOnly.FromDateTime(ahora);
            DateOnly ultimoDia = new DateOnly(hoy.Year, hoy.Month, 1).AddMonths(1).AddDays(-1);

            var existente = (await _persistence.GetFiltered<Availability>(
                    availability => availability.DoctorId == doctor.Id &&
                                    availability.Date >= hoy &&
                                    availability.Date <= ultimoDia))
                .ToList();

            var bloquesDeseados = new List<(DateOnly Date, TimeOnly StartTime, TimeOnly EndTime)>();

            for (DateOnly date = hoy; date <= ultimoDia; date = date.AddDays(1))
            {
                ParsedDay? configuredDay = days.FirstOrDefault(day => day.Day == date.DayOfWeek);
                if (configuredDay is null)
                {
                    continue;
                }

                TimeOnly current = configuredDay.StartTime;
                while (current < configuredDay.EndTime)
                {
                    TimeOnly end = current.AddMinutes(30);

                    if (date.ToDateTime(current) > ahora)
                    {
                        bloquesDeseados.Add((date, current, end));
                    }

                    current = end;
                }
            }

            bool hasOverlap = bloquesDeseados.Any(slot => existente.Any(availability =>
                availability.Date == slot.Date &&
                slot.StartTime < availability.EndTime &&
                slot.EndTime > availability.StartTime &&
                slot.StartTime != availability.StartTime &&
                (!overwrite || availability.Status == AvailabilityStatus.Reserved)));

            if (hasOverlap)
            {
                throw new ConflictException(
                        CodigoSuperposicionDisponibilidad,
                        "La disponibilidad se superpone con un horario ya configurado.")
                    .WithDetail("days", "overlapping_schedule");
            }

            if (overwrite)
            {
                var removable = existente
                    .Where(availability =>
                        availability.Status != AvailabilityStatus.Reserved &&
                        !bloquesDeseados.Any(slot =>
                            slot.Date == availability.Date &&
                            slot.StartTime == availability.StartTime))
                    .ToArray();

                await _persistence.DeleteRange(removable);

                existente = existente
                    .Except(removable)
                    .ToList();
            }

            var created = bloquesDeseados
                .Where(slot => !existente.Any(availability =>
                    availability.Date == slot.Date &&
                    availability.StartTime == slot.StartTime))
                .Select(slot => new Availability(
                    doctor,
                    slot.Date,
                    slot.StartTime,
                    slot.EndTime))
                .ToList();

            await _persistence.AddRange(created);
            await _persistence.SaveChanges();

            _logger.LogInformation(
                overwrite
                    ? "Se actualizó la disponibilidad mensual del médico {DoctorId}. Slots creados: {Count}"
                    : "Se creó disponibilidad mensual para el médico {DoctorId}. Slots creados: {Count}",
                doctor.Id,
                created.Count);

            return created.Select(Map).ToArray();
        }

        private static IReadOnlyCollection<ParsedDay> InterpretarYValidarDias(
            IReadOnlyCollection<AvailabilityModel.DayRequest>? requests)
        {
            List<(string Field, string Issue)> errors = [];
            var parsedDays = new List<ParsedDay>();

            if (requests is null || requests.Count == 0)
            {
                throw new ValidationException().WithDetail("days", "required"); 
            }

            int index = 0;
            foreach (AvailabilityModel.DayRequest request in requests)
            {
                DayOfWeek? day = ParseDay(request.Day);
                bool startValid = TimeOnly.TryParseExact(
                    request.StartTime,
                    "HH:mm",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out TimeOnly startTime);
                bool endValid = TimeOnly.TryParseExact(
                    request.EndTime,
                    "HH:mm",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out TimeOnly endTime);

                if (day is null)
                {
                    errors.Add(($"days[{index}].day", "invalid_day"));
                }

                if (!startValid)
                {
                    errors.Add(($"days[{index}].startTime", "invalid_HH_mm_format"));
                }

                if (!endValid)
                {
                    errors.Add(($"days[{index}].endTime", "invalid_HH_mm_format"));
                }

                if (day is not null && startValid && endValid)
                {
                    if (startTime >= endTime)
                    {
                        errors.Add(($"days[{index}]", "startTime_must_be_before_endTime"));
                    }
                    else if ((endTime.ToTimeSpan() - startTime.ToTimeSpan()).TotalMinutes % 30 != 0)
                    {
                        errors.Add(($"days[{index}]", "range_must_be_divisible_into_30_minute_slots"));
                    }
                    else if (parsedDays.Any(existing => existing.Day == day.Value))
                    {
                        errors.Add(($"days[{index}].day", "overlapping_or_repeated_day"));
                    }
                    else
                    {
                        parsedDays.Add(new ParsedDay(day.Value, startTime, endTime));
                    }
                }

                index++;
            }

            ServiceValidation.ThrowIfAny(errors);
            return parsedDays;
        }

        private static DayOfWeek? ParseDay(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            string normalized = RemoveDiacritics(value.Trim()).ToUpperInvariant();

            return normalized switch
            {
                "LUNES" or "MONDAY" => DayOfWeek.Monday,
                "MARTES" or "TUESDAY" => DayOfWeek.Tuesday,
                "MIERCOLES" or "WEDNESDAY" => DayOfWeek.Wednesday,
                "JUEVES" or "THURSDAY" => DayOfWeek.Thursday,
                "VIERNES" or "FRIDAY" => DayOfWeek.Friday,
                "SABADO" or "SATURDAY" => DayOfWeek.Saturday,
                "DOMINGO" or "SUNDAY" => DayOfWeek.Sunday,
                _ => null
            };
        }

        private static string RemoveDiacritics(string value)
        {
            string normalized = value.Normalize(NormalizationForm.FormD);
            return new string(normalized
                .Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                .ToArray());
        }

        private static AvailabilityModel.SlotResponse Map(Availability availability)
        {
            return new AvailabilityModel.SlotResponse(
                availability.Id,
                availability.DoctorId,
                availability.Date,
                DayName(availability.Date.DayOfWeek),
                availability.StartTime.ToString("HH:mm"),
                availability.EndTime.ToString("HH:mm"),
                availability.Status.ToString().ToUpperInvariant());
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

        private sealed record ParsedDay(DayOfWeek Day, TimeOnly StartTime, TimeOnly EndTime);
    }

}