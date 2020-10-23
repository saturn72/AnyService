using AnyService.Services;
using AnyService.Validators;
using FluentValidation.TestHelper;
using Xunit;

namespace AnyService.Tests.Validators
{
    public class EntityMappingRequestValidatorTests
    {
        [Fact]
        public void AllRules()
        {
            var model = new EntityMappingRequest();
            var v = new EntityMappingRequestModelValidator();
            var result = v.TestValidate(model);
            result.ShouldHaveValidationErrorFor(w => w.ChildEntityName);
        }
    }
}
