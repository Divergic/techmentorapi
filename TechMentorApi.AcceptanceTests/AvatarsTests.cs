﻿namespace TechMentorApi.AcceptanceTests
{
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Extensions.Logging;
    using ModelBuilder;
    using SixLabors.ImageSharp;
    using TechMentorApi.AcceptanceTests.Properties;
    using TechMentorApi.Model;
    using Xunit;
    using Xunit.Abstractions;

    public class AvatarsTests
    {
        private readonly ILogger<AvatarsTests> _logger;
        private readonly ITestOutputHelper _output;

        public AvatarsTests(ITestOutputHelper output)
        {
            _output = output;
            _logger = output.BuildLoggerFor<AvatarsTests>();
        }

        [Fact]
        public async Task PostResizedAvatarRetainingAspectRatioWhenTooLargeTest()
        {
            var account = Model.Using<ProfileBuildStrategy>().Create<Account>();
            var profile = await Model.Using<ProfileBuildStrategy>().Create<Profile>().ClearCategories()
                .Save(_logger, account).ConfigureAwait(false);
            var identity = ClaimsIdentityFactory.Build(account, profile);
            var address = ApiLocation.AccountProfileAvatars;

            var result = await Client.PostFile<AvatarDetails>(address, _logger, Resources.aspect, identity, "image/png")
                .ConfigureAwait(false);

            var location = result.Item1;

            var actual = await Client.Get<byte[]>(location, _logger).ConfigureAwait(false);

            using (var image = Image.Load(actual))
            {
                image.Height.Should().Be(300);
                image.Width.Should().Be(200);
            }
        }

        [Fact]
        public async Task PostResizedAvatarWhenTooLargeTest()
        {
            var account = Model.Using<ProfileBuildStrategy>().Create<Account>();
            var profile = await Model.Using<ProfileBuildStrategy>().Create<Profile>().ClearCategories()
                .Save(_logger, account).ConfigureAwait(false);
            var identity = ClaimsIdentityFactory.Build(account, profile);
            var address = ApiLocation.AccountProfileAvatars;

            var result = await Client.PostFile<AvatarDetails>(address, _logger, Resources.resize, identity)
                .ConfigureAwait(false);

            var location = result.Item1;

            var actual = await Client.Get<byte[]>(location, _logger).ConfigureAwait(false);

            using (var image = Image.Load(actual))
            {
                image.Height.Should().Be(300);
                image.Width.Should().Be(300);
            }
        }

        [Theory]
        [InlineData("application/octet-stream")]
        [InlineData("image/gif")]
        public async Task PostReturnsBadRequestForUnsupportedContentTypeTest(string contentType)
        {
            var account = Model.Using<ProfileBuildStrategy>().Create<Account>();
            var profile = await Model.Using<ProfileBuildStrategy>().Create<Profile>().ClearCategories()
                .Save(_logger, account).ConfigureAwait(false);
            var identity = ClaimsIdentityFactory.Build(account, profile);
            var address = ApiLocation.AccountProfileAvatars;

            await Client.PostFile<AvatarDetails>(address, _logger, Resources.avatar, identity,
                    contentType, HttpStatusCode.BadRequest)
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task PostReturnsBadRequestWhenFileTooLargeTest()
        {
            var account = Model.Using<ProfileBuildStrategy>().Create<Account>();
            var profile = await Model.Using<ProfileBuildStrategy>().Create<Profile>().ClearCategories()
                .Save(_logger, account).ConfigureAwait(false);
            var identity = ClaimsIdentityFactory.Build(account, profile);
            var address = ApiLocation.AccountProfileAvatars;

            await Client.PostFile<AvatarDetails>(address, _logger, Resources.oversize, identity, "image/jpeg",
                    HttpStatusCode.BadRequest)
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task PostReturnsBadRequestWhenNoContentProvidedTest()
        {
            var identity = ClaimsIdentityFactory.Build();
            var address = ApiLocation.AccountProfileAvatars;

            await Client
                .Post(address, _logger, null, identity, HttpStatusCode.BadRequest)
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task PostReturnsCreatedForNewAvatarTest()
        {
            var account = Model.Using<ProfileBuildStrategy>().Create<Account>();
            var profile = await Model.Using<ProfileBuildStrategy>().Create<Profile>().ClearCategories()
                .Save(_logger, account).ConfigureAwait(false);
            var identity = ClaimsIdentityFactory.Build(account, profile);
            var address = ApiLocation.AccountProfileAvatars;

            var actual = await Client.PostFile<AvatarDetails>(address, _logger, Resources.avatar, identity)
                .ConfigureAwait(false);

            var details = actual.Item2;

            details.ETag.Should().NotBeNullOrWhiteSpace();
            details.Id.Should().NotBeEmpty();
            details.ProfileId.Should().Be(profile.Id);
        }

        [Fact]
        public async Task PostReturnsLocationOfCreatedAvatarTest()
        {
            var account = Model.Using<ProfileBuildStrategy>().Create<Account>();
            var profile = await Model.Using<ProfileBuildStrategy>().Create<Profile>().ClearCategories()
                .Save(_logger, account).ConfigureAwait(false);
            var identity = ClaimsIdentityFactory.Build(account, profile);
            var address = ApiLocation.AccountProfileAvatars;
            var expected = Resources.avatar;

            var result = await Client.PostFile<AvatarDetails>(address, _logger, expected, identity)
                .ConfigureAwait(false);

            var location = result.Item1;

            var actual = await Client.Get<byte[]>(location, _logger).ConfigureAwait(false);

            actual.SequenceEqual(expected).Should().BeTrue();
        }

        [Fact]
        public async Task PostReturnsUnauthorizedForAnonymousUserTest()
        {
            var address = ApiLocation.AccountProfileAvatars;

            await Client.Post(address, _logger, null, null, HttpStatusCode.Unauthorized).ConfigureAwait(false);
        }
    }
}