using Shouldly;
using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace AnyService.Utilities.Tests.Extensions
{
    public class FuncExtensionsTests
    {
        [Fact]
        public void ConvertFunc()
        {
            var src = new Func<object, int>(x => ((int)x) * 2);
            var dest = FuncExtensions.Convert<object, int, int>(src);
            var f = dest.ShouldBeOfType<Func<int, int>>();
            f(2).ShouldBe(4);
        }
        [Fact]
        public void ConvertExpression()
        {
            Func<object, int> src = x => ((int)x) * 2;
            Expression<Func<object, int>> srcExp = y => src(y);

            var dest = FuncExtensions.Convert<object, int, int>(srcExp);
            var f = dest.Compile();
            f(2).ShouldBe(4);
        }
        [Fact]
        public void AndAlso_MultipleResult()
        {
            var records = new[]
            {
                new MyClass { Id = "1", Value = 1, Name = "name-1"},
                new MyClass { Id = "2", Value = 2, Name = "name-2"},
                new MyClass { Id = "3", Value = 3, Name = "name-3"},
            };
            var func1 = new Func<MyClass, bool>(x => x.Id != null);
            var func2 = new Func<MyClass, bool>(x => x.Value > 0);
            var func3 = new Func<MyClass, bool>(x => x.Name != null);
            var f = FuncExtensions.AndAlso(func1, func2, func3);
            var res = records.Where(f);
            res.Count().ShouldBe(records.Length);
            res.ShouldAllBe(x => records.Contains(x));
        }
        [Fact]
        public void AndAlso_NotConcatingNull()
        {
            var records = new[]
            {
                new MyClass { Id = "1", Value = 1, Name = "name-1"},
                new MyClass { Id = "2", Value = 2},
                new MyClass { Id = "3", Value = 3, Name = "name-3"},
            };
            var func1 = new Func<MyClass, bool>(x => x.Id != null);
            var func3 = new Func<MyClass, bool>(x => x.Name != null);
            var f = FuncExtensions.AndAlso(func1, func3);

            var res = records.Where(f);
            res.Count().ShouldBe(2);
            res.ShouldAllBe(x => new[] { records[0], records[2] }.Contains(x));
        }

        public class MyClass2
        {
            public string Id { get; set; }
            public bool Deleted { get; set; }
            public string Name { get; set; }
        }
        public class MyClass
        {
            public string Id { get; set; }
            public int Value { get; set; }
            public string Name { get; set; }
        }
    }
}
