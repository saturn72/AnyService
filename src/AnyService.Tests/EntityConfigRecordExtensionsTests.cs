using AutoMapper.Configuration.Annotations;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace AnyService.Tests
{
    public class EntityConfigRecordExtensionsTests
    {
        readonly IEnumerable<EntityConfigRecord> _records = new[]
            {
                new EntityConfigRecord
                {
                    Name = "name-1",
                    Type = typeof(string),
                },new EntityConfigRecord
                {
                    Name = "name-2",
                    Type = typeof(string),
                }
            };
        [Fact]
        public void FirstOrDefault_ReturnsDefault()
        {
            _records.FirstOrDefault(typeof(int)).ShouldBeNull();            
        }
        [Fact]
        public void FirstOrDefault_ReturnsInstance()
        {
            _records.FirstOrDefault(typeof(string)).Name.ShouldBe("name-1");
        }

        [Fact]
        public void First_Throws()
        {
            Should.Throw<InvalidOperationException>(() => _records.First(typeof(int)));
        }
        [Fact]
        public void First_ReturnsInstance()
        {
            _records.First(typeof(string)).Name.ShouldBe("name-1");
        }
        [Fact]
        public void All_returnsEmptyArray()
        {
            _records.All(typeof(int)).Count().ShouldBe(0);
        }
        [Fact]
        public void All_returnsAll()
        {
            var a = _records.All(typeof(string ));
            a.Count().ShouldBe(2);
            a.ElementAt(0).Name.ShouldBe("name-1");
            a.ElementAt(1).Name.ShouldBe("name-2");
        }
    }
}
