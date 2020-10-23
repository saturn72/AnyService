using AnyService.Models;
using FluentValidation;
using System;

namespace AnyService.Validators
{
    public class EntityMappingRequestModelValidator : AbstractValidator<EntityMappingRequestModel>
    {
        public EntityMappingRequestModelValidator()
        {
            RuleFor(e => e.ParentEntityKey).Must(x => x.HasValue());
            RuleFor(e => e.ChildEntityKey).Must(x => x.HasValue());
            RuleFor(e => e).Must(x => !x.ChildEntityKey.Equals(x.ParentEntityKey, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
