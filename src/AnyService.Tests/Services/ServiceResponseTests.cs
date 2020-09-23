﻿using Shouldly;
using AnyService.Services;
using Xunit;
using System;
using System.Reflection;

namespace AnyService.Tests.Services
{
    public sealed class ServiceResponseTests
    {
        [Fact]
        public void Ctor_NoParameters()
        {
            var sr = new ServiceResponse();
            sr.Message.ShouldBeNull();
            sr.ExceptionId.ShouldBeNull();
        }
        [Fact]
        public void Ctor_Duplicate()
        {
            var src = new ServiceResponse<string>
            {
                ExceptionId = "exId",
                Message = "msg",
                Result = "res",
                Payload = "pl"
            };
            var sr = new ServiceResponse(src);
            sr.Message.ShouldBe(src.Message);
            sr.ExceptionId.ShouldBe(src.ExceptionId);
            sr.Result.ShouldBe(src.Result);

            var pi = typeof(ServiceResponse).GetProperty("PayloadObject", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            var val = (string)pi.GetValue(sr);
            val.ShouldBe(src.Payload);
        }
    }
}