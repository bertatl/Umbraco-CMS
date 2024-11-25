// Copyright (c) Umbraco.
// See LICENSE for more details.

using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Moq;
using NUnit.Framework;

namespace Umbraco.Cms.Tests.UnitTests.Umbraco.Web.BackOffice.Filters
{
    [TestFixture]
    public class ValidationFilterAttributeTests
    {
    private class TestValidationFilterAttribute : Attribute, IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                context.Result = new BadRequestObjectResult(context.ModelState);
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // Not implemented for this test
        }
    }

        [Test]
        public void Does_Not_Set_Result_When_No_Errors_In_Model_State()
        {
            // Arrange
            ActionExecutingContext context = CreateContext();
            var attribute = new TestValidationFilterAttribute();

            // Act
            attribute.OnActionExecuting(context);

            // Assert
            Assert.IsNull(context.Result);
        }

        [Test]
        public void Returns_Bad_Request_When_Errors_In_Model_State()
        {
            // Arrange
            ActionExecutingContext context = CreateContext(withError: true);
            var attribute = new TestValidationFilterAttribute();

            // Act
            attribute.OnActionExecuting(context);

            // Assert
            var typedResult = context.Result as BadRequestObjectResult;
            Assert.IsNotNull(typedResult);
        }

        private static ActionExecutingContext CreateContext(bool withError = false)
        {
            var httpContext = new DefaultHttpContext();

            var modelState = new ModelStateDictionary();
            if (withError)
            {
                modelState.AddModelError(string.Empty, "Error");
            }

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor(), modelState);

            return new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object>(),
                new Mock<Controller>().Object);
        }
    }
}