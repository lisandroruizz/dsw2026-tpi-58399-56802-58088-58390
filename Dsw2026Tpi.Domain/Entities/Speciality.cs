namespace Dsw2026Tpi.Domain.Entities;

public class Speciality: EntityBase
{
    public string Name { get; private set; }
    public string Description { get; private set; }

    public ICollection<Doctor> Doctors { get; private set; } = new List<Doctor>();

    #region Constructor for EF
#pragma warning disable CS8618
    private Speciality() { }
#pragma warning restore CS8618
    #endregion

   public Speciality(string name, string description, Guid? id = null) : base(id)
    {
        Update(name, description); 
    }

    public void Update(string name, string description)
    {
        Name = name.Trim();
        Description = description.Trim(); 
    }


}
