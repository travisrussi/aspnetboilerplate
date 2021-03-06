﻿using System;
using Abp.Events.Bus;
using Abp.Events.Bus.Exceptions;
using Xunit;

namespace Abp.Web.Api.Tests.Controllers.Filters
{
    //TODO: Unit tests affect this one. If this is run alone, it works. We should fix it.
    public class AbpExceptionFilterAttribute_Tests
    {
        static AbpExceptionFilterAttribute_Tests()
        {
            AbpWebApiTests.Initialize();
        }

        [Fact]
        public void ShouldHandleExceptions()
        {
            EventBus.Default.Trigger(this, new AbpHandledExceptionData(new Exception("my exception message")));

            Assert.NotNull(MyExceptionHandler.LastException);
            Assert.Equal("my exception message", MyExceptionHandler.LastException.Message);
        }
    }
}
