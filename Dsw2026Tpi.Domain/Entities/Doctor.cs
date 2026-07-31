namespace Dsw2026Tpi.Domain.Entities;

public class Doctor: EntityBase
{
    public string Name { get; private set; }
    public string LicenseNumber { get; private set; }
    public Guid SpecialityId { get; private set; }
    public Speciality Speciality { get; private set; }
    public ICollection<Availability> Availabilities { get; private set; } = new List<Availability>();

    #region Constructor for EF
#pragma warning disable CS8618
    private Doctor()
    {
    }
#pragma warning restore CS8618
    #endregion

    public Doctor(string name, string licenseNumber, Speciality speciality, Guid? id = null) : base(id)
    {
        Update(name, licenseNumber, speciality);
    }
    public void Update(string name, string licenseNumber, Speciality speciality)
    {
        Name = name.Trim();
        LicenseNumber = licenseNumber.Trim();
        Speciality = speciality;
        SpecialityId = speciality.Id;
    }

}
