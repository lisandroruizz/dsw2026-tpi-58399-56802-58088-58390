using System;
using System.Collections.Generic;
using System.Text;
using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Helpers;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Interfaces;
using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Application.Services
{
    public class SpecialityService : ISpecialityService
    {
        private readonly IPersistence _persistence;

        public SpecialityService(IPersistence persistence)
        {
            _persistence = persistence;
        }
        public async Task<Pagination<SpecialityModel.Response>> GetAll( int pageSize,int pageIndex,string? name = null)
        {

            ServiceValidation.ValidatePagination(pageSize, pageIndex); 
            ServiceValidation.ValidateOptionalName(name);

            string? filter = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
            var specialities = await _persistence.Paginate<Speciality, string>
                (
            pageSize,
            pageIndex,
            speciality => filter == null || speciality.Name.Contains(filter),
            speciality => speciality.Name);
            return specialities.Map(Map);
        }

        public async Task<SpecialityModel.Response> Create(SpecialityModel.Request request)
        {
            Validate(request);
            var speciality = new Speciality(request.Name!, request.Description!);
            await _persistence.Add(speciality);
            await _persistence.SaveChanges();
            return Map(speciality);
        }

        public async Task<SpecialityModel.Response> Update(Guid id, SpecialityModel.Request request)
        {
         Validate(request);
            Speciality speciality = await _persistence.GetById<Speciality>(id)
         ?? throw new EntityNotFoundException( "Especialidad");
            speciality.Update(request.Name!, request.Description!);
            await _persistence.Update(speciality);
            await _persistence.SaveChanges();
            return Map(speciality);
        }

        public async Task Delete(Guid id)
        {
            Speciality speciality = await _persistence.GetById<Speciality>(id)
            ?? throw new EntityNotFoundException( "Especialidad");
            await _persistence.Delete(speciality);
            await _persistence.SaveChanges();
        }

        private static void Validate(SpecialityModel.Request request)
        {
            List<(string Field, string Issue)> errors = [];
            if (!request.Name.HasLengthBetween(3, 100))
            {
                errors.Add(("name"
                ,
                "La_longitud_debe_estar_entre_3_y_100"));
            }
            if (!request.Description.HasLengthBetween(10, 100))
            {
                errors.Add(("description"
                ,
                "La_longitud_debe_estar_entre_10_y_100"));
            }
            ServiceValidation.ThrowIfAny(errors);
        }

        private static SpecialityModel.Response Map(Speciality speciality)
        {
            return new SpecialityModel.Response(
            speciality.Id,
            speciality.Name,
            speciality.Description);
        }

    }


}

