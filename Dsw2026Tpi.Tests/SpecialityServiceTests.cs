using System;
using System.Collections.Generic;
using System.Text;
using Moq;
using Xunit;
using Dsw2026Tpi.Application.Services;
using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Domain.Interfaces;
using Dsw2026Tpi.Domain.Entities;
using System.Linq.Expressions;


namespace Dsw2026Tpi.Tests
{
    public class SpecialityServiceTests
    {
        [Fact]
        public async Task Crear_CuandoLosDatosSeanValidos_DevolverLaRespuesta()
        {
            var persistenceMock = new Mock<IPersistence>();
            var service = new SpecialityService(persistenceMock.Object);

            var request = new SpecialityModel.Request("Odontologia", "Atencion dental general");

            persistenceMock.Setup(p => p.First<Speciality>(
           It.IsAny<Expression<Func<Speciality, bool>>>(),
           It.IsAny<string[]>()))
           .ReturnsAsync((Speciality)null);

            persistenceMock.Setup(p => p.TrySaveChanges()).ReturnsAsync(true);

            var result = await service.Create(request);

            Assert.NotNull(result);
            Assert.Equal("Odontologia", result.Name);

            persistenceMock.Verify(p => p.Add(It.IsAny<Speciality>()), Times.Once);
        }
    }
}
