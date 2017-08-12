﻿namespace DevMentorApi.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks;
    using DevMentorApi.Model;
    using DevMentorApi.ViewModels;
    using FluentAssertions;
    using Microsoft.Extensions.Logging;
    using ModelBuilder;
    using Xunit;
    using Xunit.Abstractions;

    public class ProfileTests
    {
        private readonly ILogger<ProfileTests> _logger;
        private readonly ITestOutputHelper _output;

        public ProfileTests(ITestOutputHelper output)
        {
            _output = output;
            _logger = output.BuildLoggerFor<ProfileTests>();
        }

        [Fact]
        public async Task GetForNewUserCreatesProfileAsHiddenTest()
        {
            var profile = Model.Create<Profile>().Set(x => x.BannedAt = null);
            var identity = ClaimsIdentityFactory.Build(null, profile);
            var address = ApiLocation.Profile;

            var actual = await Client.Get<Profile>(address, _logger, identity).ConfigureAwait(false);

            actual.Status.Should().Be(ProfileStatus.Hidden);
        }

        [Fact]
        public async Task GetForNewUserRegistersAccountAndReturnsNewProfileTest()
        {
            var profile = new Profile
            {
                FirstName = Guid.NewGuid().ToString(),
                LastName = Guid.NewGuid().ToString(),
                Email = Guid.NewGuid().ToString("N") + "@test.com"
            };
            var identity = ClaimsIdentityFactory.Build(null, profile);
            var address = ApiLocation.Profile;

            var actual = await Client.Get<Profile>(address, _logger, identity).ConfigureAwait(false);

            actual.ShouldBeEquivalentTo(profile, opt => opt.Excluding(x => x.Id).Excluding(x => x.Status));
        }

        [Theory]
        [InlineData("email", "first", "last")]
        [InlineData(null, "first", "last")]
        [InlineData("email", null, "last")]
        [InlineData("email", "first", null)]
        public async Task GetForNewUserRegistersAccountWithProvidedClaimsTest(
            string email,
            string firstName,
            string lastName)
        {
            var profile = new Profile
            {
                Email = email,
                FirstName = firstName,
                LastName = lastName
            };
            var identity = ClaimsIdentityFactory.Build(null, profile);
            var address = ApiLocation.Profile;

            var actual = await Client.Get<Profile>(address, _logger, identity).ConfigureAwait(false);

            actual.ShouldBeEquivalentTo(profile, opt => opt.Excluding(x => x.Id));
        }

        [Fact]
        public async Task GetReturnsForbiddenForAnonymousUserTest()
        {
            var address = ApiLocation.Profile;

            await Client.Get(address, _logger, null, HttpStatusCode.Unauthorized).ConfigureAwait(false);
        }

        [Theory]
        [InlineData(2000, 2000)]
        [InlineData(2000, 2001)]
        [InlineData(null, 2000)]
        [InlineData(2000, null)]
        public async Task PutAllowsValidSkillYearRangesTest(int? yearStarted, int? yearLastUsed)
        {
            var expected = Model.Using<ProfileBuildStrategy>().Create<UpdatableProfile>();
            var skill = Model.Using<ProfileBuildStrategy>().Create<Skill>().Set(x => x.YearStarted = yearStarted)
                .Set(x => x.YearLastUsed = yearLastUsed);

            expected.Skills.Add(skill);

            var user = ClaimsIdentityFactory.Build(null, expected);

            await Client.Put(ApiLocation.Profile, _logger, expected, user, HttpStatusCode.NoContent)
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task PutDoesNotAllowOverPostingTheBannedAtValueTest()
        {
            //var profile = Model.Build<Profile>();
            //var user = ClaimsIdentityFactory.Build(null, profile);

            //await Client.Put(ApiLocation.Profile, _logger, profile, user).ConfigureAwait(false);

            //var administrator = ClaimsIdentityFactory.Build().AsAdministrator();
            throw new NotImplementedException();
        }

        [Fact]
        public async Task PutDoesNotCauseNewCategoryToBePublicallyVisibleTest()
        {
            var expected = Model.Using<ProfileBuildStrategy>().Create<UpdatableProfile>();
            var skill = Model.Using<ProfileBuildStrategy>().Create<Skill>();
            var user = ClaimsIdentityFactory.Build(null, expected);

            expected.Skills.Clear();
            expected.Skills.Add(skill);

            await Client.Put(ApiLocation.Profile, _logger, expected, user, HttpStatusCode.NoContent)
                .ConfigureAwait(false);

            var actual = await Client.Get<List<PublicCategory>>(ApiLocation.Categories, _logger).ConfigureAwait(false);

            actual.Should().NotContain(
                x => x.Group == CategoryGroup.Skill && x.Name.Equals(skill.Name, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task PutReturnsBadRequestForEmptyBodyTest()
        {
            var user = ClaimsIdentityFactory.Build();

            await Client.Put(ApiLocation.Profile, _logger, null, user, HttpStatusCode.BadRequest).ConfigureAwait(false);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("stuff")]
        public async Task PutReturnsBadRequestForInvalidEmailTest(string email)
        {
            var expected = Model.Using<ProfileBuildStrategy>().Create<UpdatableProfile>().Set(x => x.Email = email);
            var user = ClaimsIdentityFactory.Build(null, expected);

            await Client.Put(ApiLocation.Profile, _logger, expected, user, HttpStatusCode.BadRequest)
                .ConfigureAwait(false);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task PutReturnsBadRequestForInvalidFirstNameTest(string firstName)
        {
            var expected = Model.Using<ProfileBuildStrategy>().Create<UpdatableProfile>()
                .Set(x => x.FirstName = firstName);
            var user = ClaimsIdentityFactory.Build(null, expected);

            await Client.Put(ApiLocation.Profile, _logger, expected, user, HttpStatusCode.BadRequest)
                .ConfigureAwait(false);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task PutReturnsBadRequestForInvalidLastNameTest(string lastName)
        {
            var expected = Model.Using<ProfileBuildStrategy>().Create<UpdatableProfile>()
                .Set(x => x.LastName = lastName);
            var user = ClaimsIdentityFactory.Build(null, expected);

            await Client.Put(ApiLocation.Profile, _logger, expected, user, HttpStatusCode.BadRequest)
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task PutReturnsBadRequestForInvalidProfileStatusTest()
        {
            var expected = Model.Using<ProfileBuildStrategy>().Create<UpdatableProfile>()
                .Set(x => x.Status = (ProfileStatus)int.MaxValue);
            var user = ClaimsIdentityFactory.Build(null, expected);

            await Client.Put(ApiLocation.Profile, _logger, expected, user, HttpStatusCode.BadRequest)
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task PutReturnsBadRequestForInvalidSkillLevelTest()
        {
            var expected = Model.Using<ProfileBuildStrategy>().Create<UpdatableProfile>();
            var skill = Model.Using<ProfileBuildStrategy>().Create<Skill>()
                .Set(x => x.Level = (SkillLevel)int.MaxValue);

            expected.Skills.Add(skill);

            var user = ClaimsIdentityFactory.Build(null, expected);

            await Client.Put(ApiLocation.Profile, _logger, expected, user, HttpStatusCode.BadRequest)
                .ConfigureAwait(false);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task PutReturnsBadRequestForInvalidSkillNameTest(string name)
        {
            var expected = Model.Using<ProfileBuildStrategy>().Create<UpdatableProfile>();
            var skill = Model.Using<ProfileBuildStrategy>().Create<Skill>().Set(x => x.Name = name);

            expected.Skills.Add(skill);

            var user = ClaimsIdentityFactory.Build(null, expected);

            await Client.Put(ApiLocation.Profile, _logger, expected, user, HttpStatusCode.BadRequest)
                .ConfigureAwait(false);
        }

        [Theory]
        [MemberData(nameof(InvalidYearDataSource))]
        public async Task PutReturnsBadRequestForInvalidSkillYearLastUsedTest(int year)
        {
            var expected = Model.Using<ProfileBuildStrategy>().Create<UpdatableProfile>();
            var skill = Model.Using<ProfileBuildStrategy>().Create<Skill>().Set(x => x.YearLastUsed = year);

            expected.Skills.Add(skill);

            var user = ClaimsIdentityFactory.Build(null, expected);

            await Client.Put(ApiLocation.Profile, _logger, expected, user, HttpStatusCode.BadRequest)
                .ConfigureAwait(false);
        }

        [Theory]
        [MemberData(nameof(InvalidYearDataSource))]
        public async Task PutReturnsBadRequestForInvalidSkillYearStartedTest(int year)
        {
            var expected = Model.Using<ProfileBuildStrategy>().Create<UpdatableProfile>();
            var skill = Model.Using<ProfileBuildStrategy>().Create<Skill>().Set(x => x.YearStarted = year);

            expected.Skills.Add(skill);

            var user = ClaimsIdentityFactory.Build(null, expected);

            await Client.Put(ApiLocation.Profile, _logger, expected, user, HttpStatusCode.BadRequest)
                .ConfigureAwait(false);
        }

        [Theory]
        [MemberData(nameof(InvalidYearDataSource))]
        public async Task PutReturnsBadRequestForInvalidYearStartedInTechTest(int year)
        {
            var expected = Model.Using<ProfileBuildStrategy>().Create<UpdatableProfile>()
                .Set(x => x.YearStartedInTech = year);
            var user = ClaimsIdentityFactory.Build(null, expected);

            await Client.Put(ApiLocation.Profile, _logger, expected, user, HttpStatusCode.BadRequest)
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task PutReturnsBadRequestForSkillYearStartedAfterYearLastUsedTest()
        {
            var expected = Model.Using<ProfileBuildStrategy>().Create<UpdatableProfile>();
            var skill = Model.Using<ProfileBuildStrategy>().Create<Skill>().Set(x => x.YearStarted = 2001)
                .Set(x => x.YearLastUsed = 2000);

            expected.Skills.Add(skill);

            var user = ClaimsIdentityFactory.Build(null, expected);

            await Client.Put(ApiLocation.Profile, _logger, expected, user, HttpStatusCode.BadRequest)
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task PutReturnsUnauthorizedForAnonymousUserTest()
        {
            var profile = Model.Using<ProfileBuildStrategy>().Create<Profile>();

            await Client.Put(ApiLocation.Profile, _logger, profile, null, HttpStatusCode.Unauthorized)
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task PutUpdatesProfileInformationTest()
        {
            var expected = Model.Using<ProfileBuildStrategy>().Create<UpdatableProfile>();
            var user = ClaimsIdentityFactory.Build(null, expected);

            await Client.Put(ApiLocation.Profile, _logger, expected, user, HttpStatusCode.NoContent)
                .ConfigureAwait(false);

            var actual = await Client.Get<Profile>(ApiLocation.Profile, _logger, user).ConfigureAwait(false);

            actual.ShouldBeEquivalentTo(expected, opt => opt.ExcludingMissingMembers());
        }

        private static IEnumerable<object[]> InvalidYearDataSource()
        {
            yield return new object[]
            {
                1988
            };
            yield return new object[]
            {
                DateTimeOffset.UtcNow.Year + 1
            };
        }
    }
}