using Xunit;
using Dsw2026Tpi.Domain.Entities; 

namespace Dsw2026Tpi.Tests
{
    public class SpecialityTests
    {
        [Fact]
        public void Prueba() // especialidad con espacio y texto limpio.
        {
            // Arrange & Act (El constructor llama automáticamente al método Update interno).
            var especialidad = new Speciality("   Cardiología   ", "   Área de corazón   ");

            // Assert (Verificamos que la regla de negocio funcionó).
            Assert.Equal("Cardiología", especialidad.Name);
            Assert.Equal("Área de corazón", especialidad.Description);
        }
    }
}