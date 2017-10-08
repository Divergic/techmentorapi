﻿namespace TechMentorApi.UnitTests.Controllers
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Business;
    using TechMentorApi.Controllers;
    using TechMentorApi.Core;
    using FluentAssertions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Controllers;
    using Microsoft.AspNetCore.Routing;
    using Model;
    using ModelBuilder;
    using NSubstitute;
    using Xunit;

    public class ProfileControllerTests
    {
        [Fact]
        public async Task DeleteBansProfileTest()
        {
            var profile = Model.Create<Profile>();

            var manager = Substitute.For<IProfileManager>();
            var httpContext = Substitute.For<HttpContext>();

            var routerData = new RouteData();
            var actionDescriptor = new ControllerActionDescriptor();
            var actionContext = new ActionContext(httpContext, routerData, actionDescriptor);
            var controllerContext = new ControllerContext(actionContext);

            using (var tokenSource = new CancellationTokenSource())
            {
                using (var target = new ProfileController(manager))
                {
                    target.ControllerContext = controllerContext;

                    manager.BanProfile(profile.Id, Arg.Any<DateTimeOffset>(), tokenSource.Token).Returns(profile);

                    var actual = await target.Delete(profile.Id, tokenSource.Token).ConfigureAwait(false);

                    actual.Should().BeOfType<NoContentResult>();

                    await manager.Received().BanProfile(profile.Id,
                        Verify.That<DateTimeOffset>(x => x.Should().BeCloseTo(DateTimeOffset.UtcNow, 1000)),
                        tokenSource.Token).ConfigureAwait(false);
                }
            }
        }

        [Fact]
        public async Task DeleteReturnsNotFoundWhenManagerReturnsNullTest()
        {
            var id = Guid.NewGuid();

            var manager = Substitute.For<IProfileManager>();
            var httpContext = Substitute.For<HttpContext>();

            var routerData = new RouteData();
            var actionDescriptor = new ControllerActionDescriptor();
            var actionContext = new ActionContext(httpContext, routerData, actionDescriptor);
            var controllerContext = new ControllerContext(actionContext);

            using (var tokenSource = new CancellationTokenSource())
            {
                using (var target = new ProfileController(manager))
                {
                    target.ControllerContext = controllerContext;

                    var actual = await target.Delete(id, tokenSource.Token).ConfigureAwait(false);

                    actual.Should().BeOfType<ErrorMessageResult>();
                }
            }
        }

        [Fact]
        public async Task DeleteReturnsNotFoundWithEmptyIdTest()
        {
            var id = Guid.Empty;

            var manager = Substitute.For<IProfileManager>();
            var httpContext = Substitute.For<HttpContext>();

            var routerData = new RouteData();
            var actionDescriptor = new ControllerActionDescriptor();
            var actionContext = new ActionContext(httpContext, routerData, actionDescriptor);
            var controllerContext = new ControllerContext(actionContext);

            using (var tokenSource = new CancellationTokenSource())
            {
                using (var target = new ProfileController(manager))
                {
                    target.ControllerContext = controllerContext;

                    var actual = await target.Delete(id, tokenSource.Token).ConfigureAwait(false);

                    actual.Should().BeOfType<ErrorMessageResult>();
                }
            }
        }

        [Fact]
        public async Task GetReturnsNotFoundWhenManagerReturnsNullTest()
        {
            var id = Guid.NewGuid();

            var manager = Substitute.For<IProfileManager>();
            var httpContext = Substitute.For<HttpContext>();

            var routerData = new RouteData();
            var actionDescriptor = new ControllerActionDescriptor();
            var actionContext = new ActionContext(httpContext, routerData, actionDescriptor);
            var controllerContext = new ControllerContext(actionContext);

            using (var tokenSource = new CancellationTokenSource())
            {
                using (var target = new ProfileController(manager))
                {
                    target.ControllerContext = controllerContext;

                    var actual = await target.Get(id, tokenSource.Token).ConfigureAwait(false);

                    actual.Should().BeOfType<ErrorMessageResult>();
                }
            }
        }

        [Fact]
        public async Task GetReturnsNotFoundWithEmptyIdTest()
        {
            var id = Guid.Empty;

            var manager = Substitute.For<IProfileManager>();
            var httpContext = Substitute.For<HttpContext>();

            var routerData = new RouteData();
            var actionDescriptor = new ControllerActionDescriptor();
            var actionContext = new ActionContext(httpContext, routerData, actionDescriptor);
            var controllerContext = new ControllerContext(actionContext);

            using (var tokenSource = new CancellationTokenSource())
            {
                using (var target = new ProfileController(manager))
                {
                    target.ControllerContext = controllerContext;

                    var actual = await target.Get(id, tokenSource.Token).ConfigureAwait(false);

                    actual.Should().BeOfType<ErrorMessageResult>();
                }
            }
        }

        [Fact]
        public async Task GetReturnsProfileForSpecifiedIdTest()
        {
            var profile = Model.Create<PublicProfile>();

            var manager = Substitute.For<IProfileManager>();
            var httpContext = Substitute.For<HttpContext>();

            var routerData = new RouteData();
            var actionDescriptor = new ControllerActionDescriptor();
            var actionContext = new ActionContext(httpContext, routerData, actionDescriptor);
            var controllerContext = new ControllerContext(actionContext);

            using (var tokenSource = new CancellationTokenSource())
            {
                manager.GetPublicProfile(profile.Id, tokenSource.Token).Returns(profile);

                using (var target = new ProfileController(manager))
                {
                    target.ControllerContext = controllerContext;

                    var actual = await target.Get(profile.Id, tokenSource.Token).ConfigureAwait(false);

                    actual.Should().BeOfType<OkObjectResult>();

                    var result = actual.As<OkObjectResult>();

                    result.Value.Should().Be(profile);
                }
            }
        }

        [Fact]
        public void ThrowsExceptionWhenCreatedWithNullManagerTest()
        {
            Action action = () => new ProfileController(null);

            action.ShouldThrow<ArgumentNullException>();
        }
    }
}