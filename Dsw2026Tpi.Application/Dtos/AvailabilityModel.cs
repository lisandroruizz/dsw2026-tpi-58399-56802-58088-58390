using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Application.Dtos;
    public record AvailabilityModel
    {
        public record Request(Guid DoctorId, IReadOnlyCollection<DayRequest>? Days);
        public record DayRequest(string? Day, string? StartTime, string? EndTime);
        public record ScheduleResponse(string Day, string StartTime, string EndTime);
        public record SlotResponse(
            Guid Id,
            Guid DoctorId,
            DateOnly Date,
            string Day,
            string StartTime,
            string EndTime,
            string Status);
    }
