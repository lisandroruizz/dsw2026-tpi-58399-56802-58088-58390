namespace Dsw2026Tpi.Domain.Entities;

public class AvailabilityRule : EntityBase
{
    public Guid DoctorId { get; private set; }
    public Doctor Doctor { get; private set; }
    public byte Month { get; private set; }
    public short Year { get; private set; }
    public DayOfWeek DayOfWeek { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public ICollection<AvailabilitySlot> Slots { get; private set; } = new List<AvailabilitySlot>();

    #region Constructor for EF
#pragma warning disable CS8618
    private AvailabilityRule() { }
#pragma warning restore CS8618
    #endregion

    public AvailabilityRule(
        Doctor doctor,
        int month,
        int year,
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        Guid? id = null)
        : base(id)
    {
        Doctor = doctor;
        DoctorId = doctor.Id;
        Month = checked((byte)month);
        Year = checked((short)year);
        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
    }
}
