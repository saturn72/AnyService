using AnyService.Services;
using FluentValidation;
using System;

namespace AnyService.Validators
{
    public class EntityMappingRequestValidator : AbstractValidator<EntityMappingRequest>
    {
        public EntityMappingRequestValidator()
        {
            RuleFor(e => e.ChildEntityName).Must(x => x.HasValue());
        }
    }
}
