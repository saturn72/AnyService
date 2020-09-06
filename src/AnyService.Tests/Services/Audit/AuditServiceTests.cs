using AnyService.Audity;
using AnyService.Services;
using AnyService.Services.Audit;
using AutoMapper.Configuration.Annotations;
using Microsoft.Extensions.Logging;
using Shouldly;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Threading.Tasks;
using Xunit;

namespace AnyService.Tests.Services.Audit
{
    public class AuditServiceTests
    {
        private const string Create = "c",
            Read = "r",
            Update = "u",
            Delete = "d",
            Name1 = "n1",
            Name2 = "n2",
            Name3 = "n3",
            Client1 = "c1",
            User1 = "u1";

        [MemberData(nameof(BuildAuditPaginationQuery_DATA))]

        public void BuildAuditPaginationQuery(AuditPagination p, IEnumerable<int> selectedIndexes)
        {
            var a = new TestAuditService();
            var q = a.QueryBuilder(p);

            var res = _records.Where(q);
            res.Count().ShouldBe(selectedIndexes.Count());

            for (int i = 0; i < selectedIndexes.Count(); i++)
                res.ShouldContain(x => x == _records.ElementAt(i));
        }
        public static IEnumerable<object[]> BuildAuditPaginationQuery_DATA = new[]
        {
            //Ids
            new object[]{ new AuditPagination{EntityIds = new []{Create, "a" },}, new[]{0,2 }},

            //record types
            new object[]{ new AuditPagination{AuditRecordTypes = new[]{Create },}, new[]{0,1,6 }},
            new object[]{ new AuditPagination{
                AuditRecordTypes = new[]{Create, Read },}, new[]{0,1,2,6 }},
            
            //id + record types
            new object[]
            {
                new AuditPagination{
                    EntityIds = new[] { Create, "a" },
                    AuditRecordTypes = new[]{ Read },
                },
                new[]{ 2 }},

            //names
             new object[]{new AuditPagination{EntityNames= new[] { Name1, Name2 } },new[]{ 0, 2, 4, 5, 6 }},

             //names + recrd types
             new object[]{new AuditPagination{
                 AuditRecordTypes = new[]{Create},
                 EntityNames= new[] { Name1, Name2 } },new[]{ 0, 6 }},

             //clientIds
             new object[]{new AuditPagination{ClientIds= new[] { Client1 } },new[]{ 0, 2, 6 }},

             //clientIds + names
             new object[]{ new AuditPagination { ClientIds = new[] { Client1 }, EntityNames = new[]{Name1 } },new[]{ 0, 2 }},

             //user Ids
             new object[]{new AuditPagination{UserIds= new[] { User1} },new[]{ 3, 5, 7 }},

             //user Ids + record type
             new object[]{new AuditPagination
             {
                 UserIds= new[] { User1},
                 AuditRecordTypes = new[]{Delete}
             },new[]{ 3,}},

              //client Ids
             new object[]{new AuditPagination{ ClientIds = new[] { Client1} },new[]{0, 2, 6 }},

             //clientIds + names
             new object[]{new AuditPagination
             {
                 ClientIds= new[] { Client1},
                 EntityNames = new[]{Name2}
             },new[]{ 6,}},

             //from
             new object[]{new AuditPagination{ FromUtc = DateTime.UtcNow.Subtract(TimeSpan.FromDays(3))},new[]{7, 8 }},

             // from + record type
             new object[]
             {
                 new AuditPagination{
                     FromUtc = DateTime.UtcNow.Subtract(TimeSpan.FromDays(3)),
                     AuditRecordTypes = new[]{Delete }
                 },new[]{8 }},


             //to
             new object[]{new AuditPagination{ ToUtc = DateTime.UtcNow.Subtract(TimeSpan.FromDays(3))}
             ,new[]{0, 1, 2, 3, 4, 5, 6,}},

             // from + to
             new object[]
             {
                 new AuditPagination{
                     ToUtc = DateTime.UtcNow.Subtract(TimeSpan.FromDays(3)),
                     FromUtc = DateTime.UtcNow.Subtract(TimeSpan.FromDays(10)),
                 },new[]{1 }},


        };
        private readonly IEnumerable<AuditRecord> _records = new[]
        {
            new AuditRecord {Id = "a",  AuditRecordType = Create, EntityName = Name1, ClientId = Client1},
            new AuditRecord {Id = "b",  AuditRecordType = Create, EntityName = Name3, OnUtc = DateTime.UtcNow.Subtract(TimeSpan.FromDays(5)).ToIso8601()},
            new AuditRecord {Id = "c",  AuditRecordType = Read,  EntityName = Name1, ClientId = Client1},
            new AuditRecord {Id = "d",  AuditRecordType = Update,  EntityName = Name3, UserId = User1},
            new AuditRecord {Id = "e",  AuditRecordType = Delete,  EntityName = Name2},
            new AuditRecord {Id = "f",  AuditRecordType = Delete,  EntityName = Name1, UserId = User1},
            new AuditRecord {Id = "g",  AuditRecordType = Create,  EntityName = Name2, ClientId = Client1},
            new AuditRecord {Id = "h",  AuditRecordType = Update,  EntityName = Name3, UserId = User1, OnUtc= DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(10)).ToIso8601()},
            new AuditRecord {Id = "i",  AuditRecordType = Delete,  EntityName = Name3, OnUtc= DateTime.UtcNow.Subtract(TimeSpan.FromDays(1)).ToIso8601()},
        };

        public class TestAuditService : AuditService
        {
            public TestAuditService() : base(null, null, null, null)
            {
            }
            public Func<AuditRecord, bool> QueryBuilder(AuditPagination pagination) => BuildAuditPaginationQuery(pagination);
        }
    }
}
