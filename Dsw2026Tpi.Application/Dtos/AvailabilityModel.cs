using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Application.Dtos;
    public record AvailabilityModel
    {
        public record Request(Guid DoctorId, IReadOnlyCollection<DayRequest>? Days);
        public record DayRequest(string? Day, string? StartTime, string? EndTime);
        public record ScheduleResponse(Guid id, string Day, string StartTime, string EndTime);
        public record SlotResponse(
            Guid Id,
            Guid DoctorId,
            Guid? AvailabilityRuleId,
            DateOnly Date,
            string Day,
            string StartTime,
            string EndTime,
            string Status);

        public record Response(
        Guid DoctorId,
        int Month,
        int Year,
        IReadOnlyCollection<ScheduleResponse> Days,
        IReadOnlyCollection<SlotResponse> Slots);
}
