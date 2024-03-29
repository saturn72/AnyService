﻿using AnyService.Services.Audit;
using Shouldly;
using System.Linq;

namespace AnyService.Tests.Services.Audit
{
    public class AuditRecordTypesTests
    {
        public void All()
        {
            AuditRecordTypes.All.Count().ShouldBe(4);

            AuditRecordTypes.CREATE.ShouldBe("create");
            AuditRecordTypes.READ.ShouldBe("read");
            AuditRecordTypes.UPDATE.ShouldBe("update");
            AuditRecordTypes.DELETE.ShouldBe("delete");
        }
    }
}
