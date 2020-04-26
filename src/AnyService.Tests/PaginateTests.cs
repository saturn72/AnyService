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
            new Pagination<TestClass>().SortOrder.ShouldBe("asc");
        }
        [Fact]
        public void ctor_Query()
        {
            var q = "t.Id == 123";
            var p = new Pagination<TestClass>(q);
            p.Query.ShouldBe(q);
            p.SortOrder.ShouldBe("asc");
        }
    }
}