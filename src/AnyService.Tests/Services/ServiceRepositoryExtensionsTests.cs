using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AnyService.Core;
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
        #region Query
        [Fact]
        public async Task Query_RepositoryThrows()
        {
            var repo = new Mock<IRepository<TestClass>>();
            var ex = new Exception();
            repo.Setup(r => r.GetById(It.IsAny<string>())).Throws(ex);
            var sr = new ServiceResponse();
            var w = new ServiceResponseWrapper(sr);
            var res = await ServiceRepositoryExtensions.Query(repo.Object, r => r.GetById("some-id"), w);
            res.ShouldBeNull();
            sr.Data.ShouldBeNull();
            sr.Result.ShouldBe(ServiceResult.Error);
            sr.Message.ShouldNotBeNullOrEmpty();
            w.Exception.ShouldBe(ex);
        }
        [Fact]
        public async Task Query_NotFound_SingleItem()
        {
            var repo = new Mock<IRepository<TestClass>>();
            repo.Setup(r => r.GetById(It.IsAny<string>())).ReturnsAsync(null as TestClass);
            var sr = new ServiceResponse();

            var w = new ServiceResponseWrapper(sr);
            var res = await ServiceRepositoryExtensions.Query(repo.Object, r => r.GetById("some-id"), w);
            res.ShouldBeNull();
            sr.Data.ShouldBeNull();
            sr.Result.ShouldBe(ServiceResult.NotFound);
            sr.Message.ShouldNotBeNullOrEmpty();
        }

        public static IEnumerable<object[]> Query_Empty_Collection_DATA =>
    new[]{
        new object[]{ null as IEnumerable<TestClass>},
        new object[]{ new TestClass[]{}},
};
        [Theory]
        [MemberData(nameof(Query_Empty_Collection_DATA))]
        public async Task Query_Empty_Collection(IEnumerable<TestClass> dbData)
        {
            var repo = new Mock<IRepository<TestClass>>();
            repo.Setup(r => r.GetAll(null)).ReturnsAsync(dbData);
            var sr = new ServiceResponse();

            var w = new ServiceResponseWrapper(sr);
            var res = await ServiceRepositoryExtensions.Query(repo.Object, r => r.GetAll(null), w);
            res.ShouldBe(dbData);
            sr.Data.ShouldBe(dbData);
            sr.Result.ShouldBe(ServiceResult.NotSet);
            sr.Message.ShouldBeNullOrEmpty();
        }
        [Fact]
        public async Task Query_NotFound_Paginate_ReturnsNull()
        {
            var repo = new Mock<IRepository<TestClass>>();
            repo.Setup(r => r.GetAll(It.IsAny<Pagination<TestClass>>())).ReturnsAsync(null as IEnumerable<TestClass>);
            var sr = new ServiceResponse();

            var w = new ServiceResponseWrapper(sr);
            var p = new Pagination<TestClass>();
            var res = await ServiceRepositoryExtensions.Query(repo.Object, r => r.GetAll(p), w);
            res.ShouldBeNull();
        }
        [Fact]
        public async Task Query_Found()
        {
            var dbData = new[] { new TestClass() };
            var repo = new Mock<IRepository<TestClass>>();
            repo.Setup(r => r.GetAll(null)).ReturnsAsync(dbData);
            var sr = new ServiceResponse();

            var w = new ServiceResponseWrapper(sr);
            var res = await ServiceRepositoryExtensions.Query(repo.Object, r => r.GetAll(null), w);
            res.ShouldBe(dbData);
            sr.Data.ShouldBe(dbData);
            sr.Result.ShouldBe(ServiceResult.NotSet);
            sr.Message.ShouldBeNullOrEmpty();
        }

        #endregion#region Command
        [Fact]
        public async Task Command_RepositoryThrows()
        {
            var repo = new Mock<IRepository<TestClass>>();
            var ex = new Exception();
            repo.Setup(r => r.Insert(It.IsAny<TestClass>())).Throws(ex);
            var sr = new ServiceResponse();

            var tc = new TestClass();
            var w = new ServiceResponseWrapper(sr);

            var res = await ServiceRepositoryExtensions.Command(repo.Object, r => r.Insert(tc), w);
            res.ShouldBeNull();
            sr.Data.ShouldBeNull();
            sr.Result.ShouldBe(ServiceResult.Error);
            sr.Message.ShouldNotBeNullOrEmpty();
            w.Exception.ShouldBe(ex);
        }
        [Fact]
        public async Task Command_RepositoryReturnsNull()
        {
            var repo = new Mock<IRepository<TestClass>>();
            repo.Setup(r => r.Insert(It.IsAny<TestClass>())).ReturnsAsync(null as TestClass);
            var sr = new ServiceResponse();

            var tc = new TestClass();
            var w = new ServiceResponseWrapper(sr);
            var res = await ServiceRepositoryExtensions.Command(repo.Object, r => r.Insert(tc), w);
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

            var w = new ServiceResponseWrapper(sr);
            var res = await ServiceRepositoryExtensions.Command(repo.Object, r => r.Insert(tc), w);
            res.ShouldBe(tc);
            sr.Data.ShouldBe(tc);
            sr.Result.ShouldBe(ServiceResult.NotSet);
            sr.Message.ShouldBeNullOrEmpty();
        }
    }
}