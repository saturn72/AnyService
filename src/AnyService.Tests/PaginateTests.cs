using Xunit;
using Shouldly;
using System;
using AnyService.Services;
using AnyService.Core;

namespace AnyService.Tests
{
    public class PaginateTests
    {
        public class TestClass : IDomainModelBase
        {
            public string Id { get; set; }
        }
        [Fact]
        public void ctor()
        {
            new Paginate<TestClass>().SortOrder.ShouldBe("asc");
        }
        [Fact]
        public void ctor_Query()
        {
            var q = new Func<TestClass, bool>(t => t.Id == "123");
            var p = new Paginate<TestClass>(q);
            p.Query.ShouldBe(q);
            p.SortOrder.ShouldBe("asc");
        }
    }
}