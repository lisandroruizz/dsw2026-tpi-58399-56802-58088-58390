using Dsw2026Tpi.Domain.Enums;

namespace Dsw2026Tpi.Domain.Entities;

public class AvailabilitySlot : EntityBase
{
    public Guid? AvailabilityRuleId { get; private set; }
    public AvailabilityRule? AvailabilityRule { get; private set; }
    public Guid DoctorId { get; private set; }
    public Doctor Doctor { get; private set; }
    public DateOnly SlotDate { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public AvailabilityStatus Status { get; private set; }
    public ICollection<Appointment> Appointments { get; private set; } = new List<Appointment>();

    #region Constructor for EF
#pragma warning disable CS8618
    private AvailabilitySlot() { }
#pragma warning restore CS8618
    #endregion

    public AvailabilitySlot(
        AvailabilityRule availabilityRule,
        DateOnly slotDate,
        TimeOnly startTime,
        TimeOnly endTime,
        Guid? id = null)
        : base(id)
    {
        AvailabilityRule = availabilityRule;
        AvailabilityRuleId = availabilityRule.Id;
        Doctor = availabilityRule.Doctor;
        DoctorId = availabilityRule.DoctorId;
        SlotDate = slotDate;
        StartTime = startTime;
        EndTime = endTime;
        Status = AvailabilityStatus.Available;
    }

    public DateTime GetStartDateTime()
    {
        return SlotDate.ToDateTime(StartTime);
    }

    public void Book()
    {
        if (Status != AvailabilityStatus.Available)
        {
            throw new InvalidOperationException("La disponibilidad no se encuentra libre.");
        }

        Status = AvailabilityStatus.Booked;
    }

    public void Release()
    {
        if (Status != AvailabilityStatus.Booked)
        {
            throw new InvalidOperationException("Solo se puede liberar una disponibilidad reservada.");
        }

        Status = AvailabilityStatus.Available;
    }

    public void Block()
    {
        if (Status != AvailabilityStatus.Available)
        {
            throw new InvalidOperationException("Solo se puede bloquear una disponibilidad libre.");
        }

        Status = AvailabilityStatus.Blocked;
    }

    public void AttachToRule(
        AvailabilityRule availabilityRule)
    {
        if (availabilityRule.DoctorId != DoctorId)
        {
            throw new InvalidOperationException(
                "La regla debe pertenecer al mismo médico que el slot.");
        }

        AvailabilityRule = availabilityRule;
        AvailabilityRuleId = availabilityRule.Id;
    }

    public void DetachFromRule()
    {
        AvailabilityRule = null;
        AvailabilityRuleId = null;
    }
}
