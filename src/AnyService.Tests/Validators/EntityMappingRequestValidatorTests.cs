using AnyService.Models;
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
            var model = new EntityMappingRequestModel();
            var v = new EntityMappingRequestModelValidator();
            var result = v.TestValidate(model);
            result.ShouldHaveValidationErrorFor(w => w.ChildEntityKey);
        }
    }
}
