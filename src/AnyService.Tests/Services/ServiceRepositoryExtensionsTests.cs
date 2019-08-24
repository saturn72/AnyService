using System;
using System.Threading.Tasks;
using AnyService.Services;
using Moq;
using Shouldly;
using Xunit;

namespace AnyService.Tests.Services
{
    public class ServiceRepositoryExtensionsTests
    {
        public class TestClass : IDomainModelBase
        {
            public string Id { get; set; }
        }
        [Fact]
        public async Task Command_RepositoryThrows()
        {
            var repo = new Mock<IRepository<TestClass>>();
            repo.Setup(r => r.Insert(It.IsAny<TestClass>())).Throws<Exception>();
            var sr = new ServiceResponse();

            var tc = new TestClass();
            var res = await ServiceRepositoryExtensions.Command(repo.Object, r => r.Insert(tc), sr);
            res.ShouldBeNull();
            sr.Data.ShouldBeNull();
            sr.Result.ShouldBe(ServiceResult.Error);
            sr.Message.ShouldNotBeNullOrEmpty();
        }
        [Fact]
        public async Task Command_RepositoryReturnsNull()
        {
            var repo = new Mock<IRepository<TestClass>>();
            repo.Setup(r => r.Insert(It.IsAny<TestClass>())).ReturnsAsync(null as TestClass);
            var sr = new ServiceResponse();

            var tc = new TestClass();
            var res = await ServiceRepositoryExtensions.Command(repo.Object, r => r.Insert(tc), sr);
            res.ShouldBeNull();
            sr.Data.ShouldBeNull();
            sr.Result.ShouldBe(ServiceResult.BadOrMissingData);
            sr.Message.ShouldNotBeNullOrEmpty();
        }
        [Fact]
        public async Task Command_RepositoryReturnsSavedObject()
        {
            var tc = new TestClass();
            var repo = new Mock<IRepository<TestClass>>();
            repo.Setup(r => r.Insert(It.IsAny<TestClass>())).ReturnsAsync(tc);
            var sr = new ServiceResponse();

            var res = await ServiceRepositoryExtensions.Command(repo.Object, r => r.Insert(tc), sr);
            res.ShouldBe(tc);
            sr.Data.ShouldBe(tc);
            sr.Result.ShouldBe(ServiceResult.NotSet);
            sr.Message.ShouldBeNullOrEmpty();
        }
    }
}