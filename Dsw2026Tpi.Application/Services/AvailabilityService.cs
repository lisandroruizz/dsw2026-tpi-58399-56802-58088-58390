using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;
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
        private readonly INonWorkingDayProvider _nonWorkingDayProvider;
        private const string CodigoSuperposicionDisponibilidad = "AVAILABILITY_OVERLAP";

        public AvailabilityService(IPersistence persistence, ILogger<AvailabilityService> logger, INonWorkingDayProvider nonWorkingDayProvider)
        {
            _persistence = persistence;
            _logger = logger;
            _nonWorkingDayProvider = nonWorkingDayProvider;
        }

        public Task<AvailabilityModel.Response> Create(AvailabilityModel.Request request)
        {
            return GuardarCronogramaMensual(request, overwrite: false);
        }

        public Task<AvailabilityModel.Response> Update(AvailabilityModel.Request request)
        {
            return GuardarCronogramaMensual(request, overwrite: true);
        }

        public async Task<IReadOnlyCollection<AvailabilityModel.SlotResponse>> GetAvailable(
            Guid doctorId,
            DateOnly? date = null)
        {
            _ = await _persistence.GetById<Doctor>(doctorId)
                ?? throw new EntityNotFoundException(ErrorCodeNames.DoctorNotFound,
        "Médico");

            DateOnly hoy = DateOnly.FromDateTime(DateTime.Today);
            DateOnly ultimoDia = new DateOnly(hoy.Year, hoy.Month, 1).AddMonths(1).AddDays(-1);
            DateOnly desde = date ?? hoy;
            DateOnly hasta = date ?? ultimoDia;
            DateTime ahora = DateTime.Now;

            IEnumerable<AvailabilitySlot> slots = await _persistence.GetFiltered<AvailabilitySlot>(
                  slot =>
                      slot.DoctorId == doctorId
                      && slot.SlotDate >= desde
                      && slot.SlotDate <= hasta
                      && slot.Status == AvailabilityStatus.Available);

            return slots
                .Where(slot => slot.GetStartDateTime() > ahora)
                .OrderBy(slot => slot.SlotDate)
                .ThenBy(slot => slot.StartTime)
                .Select(MapSlot)
                .ToArray();
        }

        private async Task<AvailabilityModel.Response> GuardarCronogramaMensual(
            AvailabilityModel.Request request,
            bool overwrite)
        {
            Doctor doctor = await _persistence.GetById<Doctor>(request.DoctorId)
                ?? throw new EntityNotFoundException(ErrorCodeNames.DoctorNotFound,
        "Médico");

            IReadOnlyCollection<ParsedDay> days = ParseAndValidateDays(request.Days);

            DateTime ahora = DateTime.Now;
            DateOnly hoy = DateOnly.FromDateTime(ahora);
            DateOnly firstDay = new(hoy.Year, hoy.Month, 1);
            DateOnly ultimoDia = new DateOnly(hoy.Year, hoy.Month, 1).AddMonths(1).AddDays(-1);

            List<AvailabilityRule> existingRules = (await _persistence.GetFiltered<AvailabilityRule>(
                            rule => rule.DoctorId == doctor.Id && rule.Month == hoy.Month && rule.Year == hoy.Year)).ToList();

            List<AvailabilitySlot> existingSlots = (await _persistence.GetFiltered<AvailabilitySlot>(
                    slot => slot.DoctorId == doctor.Id && slot.SlotDate >= firstDay && slot.SlotDate <= ultimoDia)).ToList();

            var newRules = new List<AvailabilityRule>();
            var rulesForGeneration = new List<AvailabilityRule>();

            if (overwrite)
            {
                foreach (AvailabilitySlot slot in existingSlots)
                {
                    if (slot.Status == AvailabilityStatus.Booked)
                    {
                        slot.DetachFromRule();
                        await _persistence.Update(slot);
                    }
                    else
                    {
                        await _persistence.Delete(slot);
                    }
                }

                await _persistence.DeleteRange(existingRules);

                foreach (ParsedDay day in days)
                {
                    var rule = new AvailabilityRule(doctor, hoy.Month, hoy.Year, day.Day, day.StartTime, day.EndTime);

                    newRules.Add(rule);
                    rulesForGeneration.Add(rule);
                }
            }
            else
            {
                foreach (ParsedDay day in days)
                {
                    AvailabilityRule? exact = existingRules.FirstOrDefault(rule =>
                        rule.DayOfWeek == day.Day && rule.StartTime == day.StartTime && rule.EndTime == day.EndTime);

                    bool overlaps = existingRules.Any(rule =>
                        rule.DayOfWeek == day.Day
                        && day.StartTime < rule.EndTime
                        && day.EndTime > rule.StartTime
                        && rule != exact);

                    if (overlaps)
                    {
                        throw new ConflictException(
                                ErrorCodeNames.AvailabilityOverlap,
                                "La disponibilidad se superpone con un horario ya configurado.")
                            .WithDetail("days", "overlapping_schedule");
                    }

                    if (exact is not null)
                    {
                        rulesForGeneration.Add(exact);
                    }
                    else
                    {
                        var rule = new AvailabilityRule(
                            doctor,
                            hoy.Month,
                            hoy.Year,
                            day.Day,
                            day.StartTime,
                            day.EndTime);

                        newRules.Add(rule);
                        rulesForGeneration.Add(rule);
                    }
                }
            }

            var createdSlots = new List<AvailabilitySlot>();

            IEnumerable<AvailabilitySlot> slotsThatRemain = overwrite
                ? existingSlots.Where(slot => slot.Status == AvailabilityStatus.Booked)
                : existingSlots;

            for (DateOnly date = hoy; date <= ultimoDia; date = date.AddDays(1))
            {
                if (_nonWorkingDayProvider.IsNonWorkingDay(date))
                {
                    continue;
                }

                IEnumerable<AvailabilityRule> rulesForDate =
                    rulesForGeneration.Where(rule => rule.DayOfWeek == date.DayOfWeek);
                foreach (AvailabilityRule rule in rulesForDate)
                {
                    TimeOnly current = rule.StartTime;

                    while (current < rule.EndTime)
                    {
                        TimeOnly end = current.AddMinutes(30);
                        bool isFuture = date.ToDateTime(current) > ahora;

                        AvailabilitySlot? existingSlot = slotsThatRemain.FirstOrDefault(
                            slot => slot.SlotDate == date && slot.StartTime == current);

                        bool alreadyCreated = createdSlots.Any(
                            slot => slot.SlotDate == date && slot.StartTime == current);

                        if (isFuture
                            && existingSlot is not null
                            && existingSlot.AvailabilityRuleId is null
                            && existingSlot.Status != AvailabilityStatus.Booked)
                        {
                            existingSlot.AttachToRule(rule);
                            await _persistence.Update(existingSlot);
                        }

                        if (isFuture && existingSlot is null && !alreadyCreated)
                        {
                            createdSlots.Add(new AvailabilitySlot(rule, date, current, end));
                        }

                        current = end;
                    }
                }
            }

            await _persistence.AddRange(newRules);
            await _persistence.AddRange(createdSlots);
            await _persistence.SaveChanges();

            _logger.LogInformation(
                overwrite
                    ? "Se reemplazó la disponibilidad mensual del médico {DoctorId}. Reglas: {RuleCount}. Slots nuevos: {SlotCount}"
                    : "Se creó disponibilidad mensual para el médico {DoctorId}. Reglas nuevas: {RuleCount}. Slots nuevos: {SlotCount}",
                doctor.Id,
                newRules.Count,
                createdSlots.Count);

            return await BuildResponse(doctor.Id, hoy.Month, hoy.Year, firstDay, ultimoDia);
        }

        private async Task<AvailabilityModel.Response> BuildResponse(
            Guid doctorId,
            int month,
            int year,
            DateOnly firstDay,
            DateOnly lastDay)
        {
            IReadOnlyCollection<AvailabilityModel.ScheduleResponse> rules = (await _persistence
                    .GetFiltered<AvailabilityRule>(rule => rule.DoctorId == doctorId && rule.Month == month && rule.Year == year))
                .OrderBy(rule => DayOrder(rule.DayOfWeek))
                .ThenBy(rule => rule.StartTime)
                .Select(MapRule)
                .ToArray();

            IReadOnlyCollection<AvailabilityModel.SlotResponse> slots = (await _persistence
                    .GetFiltered<AvailabilitySlot>(slot => slot.DoctorId == doctorId && slot.SlotDate >= firstDay && slot.SlotDate <= lastDay))
                .OrderBy(slot => slot.SlotDate)
                .ThenBy(slot => slot.StartTime)
                .Select(MapSlot)
                .ToArray();

            return new AvailabilityModel.Response(doctorId, month, year, rules, slots);
        }

        private static IReadOnlyCollection<ParsedDay> ParseAndValidateDays(
            IReadOnlyCollection<AvailabilityModel.DayRequest>? requests)
        {
            List<(string Field, string Issue)> errors = [];
            var parsedDays = new List<ParsedDay>();

            if (requests is null || requests.Count == 0)
            {
                throw new ValidationException([("days", "required")]);
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
                    else if (!IsOnHalfHourGrid(startTime) || !IsOnHalfHourGrid(endTime))
                    {
                        errors.Add(($"days[{index}]", "times_must_use_00_or_30_minutes"));
                    }
                    else if ((endTime.ToTimeSpan() - startTime.ToTimeSpan()).TotalMinutes % 30 != 0)
                    {
                        errors.Add(($"days[{index}]", "range_must_be_divisible_into_30_minute_slots"));
                    }
                    else if (parsedDays.Any(existing =>
                        existing.Day == day.Value && startTime < existing.EndTime && endTime > existing.StartTime))
                    {
                        errors.Add(($"days[{index}]", "overlapping_schedule"));
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

        private static bool IsOnHalfHourGrid(TimeOnly time)
        {
            return time.Second == 0 && time.Millisecond == 0 && (time.Minute == 0 || time.Minute == 30);
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

        private static AvailabilityModel.ScheduleResponse MapRule(AvailabilityRule rule)
        {
            return new AvailabilityModel.ScheduleResponse(
                rule.Id,
                DayName(rule.DayOfWeek),
                rule.StartTime.ToString("HH:mm"),
                rule.EndTime.ToString("HH:mm"));
        }

        private static AvailabilityModel.SlotResponse MapSlot(AvailabilitySlot slot)
        {
            return new AvailabilityModel.SlotResponse(
                slot.Id,
                slot.DoctorId,
                slot.AvailabilityRuleId,
                slot.SlotDate,
                DayName(slot.SlotDate.DayOfWeek),
                slot.StartTime.ToString("HH:mm"),
                slot.EndTime.ToString("HH:mm"),
                StatusName(slot.Status));
        }

        private static string StatusName(AvailabilityStatus status)
        {
            return status.ToString().ToUpperInvariant();
        }

        private static string DayName(DayOfWeek day)
        {
            return day switch
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
        }

        private static int DayOrder(DayOfWeek day)
        {
            return day == DayOfWeek.Sunday ? 7 : (int)day;
        }

        private sealed record ParsedDay(DayOfWeek Day, TimeOnly StartTime, TimeOnly EndTime);
    }
}