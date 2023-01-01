using AnyService.Services;
using System.Linq.Expressions;

namespace AnyService.Tests
{
    public class PaginationTests
    {
        public class TestClass : IEntity
        {
            public string Id { get; set; }
        }
        [Fact]
        public void ctor()
        {
            new Pagination<TestClass>().SortOrder.ShouldBe("asc");
        }
        [Fact]
        public void ctor_IleggalQuery()
        {
            var q = "xxx";
            var p = new Pagination<TestClass>(q);
            p.QueryOrFilter.ShouldBe(q);
            p.QueryFunc.ShouldBeNull();
        }

        [Fact]
        public void ctor1()
        {
            var p = new Pagination<TestClass>();
            p.QueryOrFilter.ShouldBeNull();
            p.QueryFunc.ShouldBeNull();
            p.SortOrder.ShouldBe("asc");
        }
        [Fact]
        public void ctor2()
        {
            var q = "Id == 123";
            var p = new Pagination<TestClass>(q);
            p.QueryOrFilter.ShouldBe(q);
            p.QueryFunc.ShouldBeNull();
            p.SortOrder.ShouldBe("asc");
        }
        [Fact]
        public void ctor3()
        {
            var f = new Func<TestClass, bool>(x => x.Id == "123");
            Expression<Func<TestClass, bool>> q = (a => f(a));
            var p = new Pagination<TestClass>(q);
            p.QueryOrFilter.ShouldNotBeNull();
            p.QueryFunc.ShouldBeOfType<Func<TestClass, bool>>();
            p.SortOrder.ShouldBe("asc");
        }
    }
}