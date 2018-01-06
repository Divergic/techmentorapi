﻿namespace TechMentorApi.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Extensions.Logging;
    using Microsoft.WindowsAzure.Storage;
    using ModelBuilder;
    using Newtonsoft.Json;
    using TechMentorApi.Model;
    using TechMentorApi.ViewModels;
    using Xunit;
    using Xunit.Abstractions;

    public class AccountProfileTests
    {
        private readonly ILogger<AccountProfileTests> _logger;
        private readonly ITestOutputHelper _output;

        public AccountProfileTests(ITestOutputHelper output)
        {
            _output = output;
            _logger = output.BuildLoggerFor<AccountProfileTests>();
        }

        public static IEnumerable<object[]> InvalidYearDataSource()
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

        [Fact]
        public async Task GetForNewUserCreatesProfileAsHiddenTest()
        {
            var profile = Model.Using<ProfileBuildStrategy>().Create<Profile>();
            var identity = ClaimsIdentityFactory.Build(null, profile);
            var address = ApiLocation.AccountProfile;

            var actual = await Client.Get<Profile>(address, _logger, identity).ConfigureAwait(false);

            actual.Status.Should().Be(ProfileStatus.Hidden);
        }

        [Fact]
        public async Task GetForNewUserHandlesMultipleConcurrentRequestsWhenAccountCreationRequiredTest()
        {
            for (var index = 0; index < 3; index++)
            {
                _output.WriteLine("Executing attempt " + (index + 1));

                // Try this a few times as it might not always work in a single attempt
                var profile = Model.Using<ProfileBuildStrategy>().Create<Profile>().Set(x => x.BannedAt = null);
                var identity = ClaimsIdentityFactory.Build(null, profile);
                var address = ApiLocation.AccountProfile;

                var firstTask = Client.Get<Profile>(address, _logger, identity);
                var secondTask = Client.Get<Profile>(address, _logger, identity);
                var thirdTask = Client.Get<Profile>(address, _logger, identity);

                await Task.WhenAll(firstTask, secondTask, thirdTask).ConfigureAwait(false);
            }
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
            var address = ApiLocation.AccountProfile;

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
            var address = ApiLocation.AccountProfile;

            var actual = await Client.Get<Profile>(address, _logger, identity).ConfigureAwait(false);

            actual.ShouldBeEquivalentTo(profile, opt => opt.Excluding(x => x.Id));
        }

        [Fact]
        public async Task GetHandlesRaceConditionWithMultipleCallsOnNewAccountTest()
        {
            // Related to Issue 35
            for (var index = 0; index < 50; index++)
            {
                _output.WriteLine("Executing test " + (index + 1));

                var profile = Model.Using<ProfileBuildStrategy>().Create<Profile>();
                var identity = ClaimsIdentityFactory.Build(null, profile);
                var profileAddress = ApiLocation.AccountProfile;
                var categoryAddress = ApiLocation.Categories;

                var profileTask = Client.Get<Profile>(profileAddress, null, identity);
                var tasks = new List<Task>
                {
                    profileTask
                };

                for (var categoryCount = 0; categoryCount < 10; categoryCount++)
                {
                    var categoryTask = Client.Get<List<Category>>(categoryAddress, null, identity);

                    tasks.Add(categoryTask);
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task GetReturnsBannedProfileTest()
        {
            var account = Model.Using<ProfileBuildStrategy>().Create<Account>();
            var profile = await Model.Using<ProfileBuildStrategy>().Create<Profile>()
                .Set(x => x.BannedAt = DateTimeOffset.UtcNow).ClearCategories().Save(_logger, account)
                .ConfigureAwait(false);
            var identity = ClaimsIdentityFactory.Build(account, profile);
            var address = ApiLocation.AccountProfile;

            var actual = await Client.Get<Profile>(address, _logger, identity).ConfigureAwait(false);

            actual.ShouldBeEquivalentTo(profile, opt => opt.Excluding(x => x.Id).Excluding(x => x.BannedAt));
            actual.BannedAt.Should().BeCloseTo(profile.BannedAt.Value, 20000);
        }

        [Fact]
        public async Task GetReturnsExistingProfileTest()
        {
            var account = Model.Using<ProfileBuildStrategy>().Create<Account>();
            var profile = await Model.Using<ProfileBuildStrategy>().Create<Profile>().Save(_logger, account)
                .ConfigureAwait(false);
            var identity = ClaimsIdentityFactory.Build(account, profile);
            var address = ApiLocation.AccountProfile;

            var actual = await Client.Get<Profile>(address, _logger, identity).ConfigureAwait(false);

            actual.ShouldBeEquivalentTo(profile, opt => opt.Excluding(x => x.Id));
        }

        [Fact]
        public async Task GetReturnsForbiddenForAnonymousUserTest()
        {
            var address = ApiLocation.AccountProfile;

            await Client.Get(address, _logger, null, HttpStatusCode.Unauthorized).ConfigureAwait(false);
        }

        [Fact]
        public async Task GetReturnsHiddenProfileTest()
        {
            var account = Model.Using<ProfileBuildStrategy>().Create<Account>();
            var profile = await Model.Using<ProfileBuildStrategy>().Create<Profile>()
                .Set(x => x.Status = ProfileStatus.Hidden).ClearCategories().Save(_logger, account)
                .ConfigureAwait(false);
            var identity = ClaimsIdentityFactory.Build(account, profile);
            var address = ApiLocation.AccountProfile;

            var actual = await Client.Get<Profile>(address, _logger, identity).ConfigureAwait(false);

            actual.ShouldBeEquivalentTo(profile, opt => opt.Excluding(x => x.Id));
        }

        [Theory]
        [InlineData("C#")]
        [InlineData("C++")]
        [InlineData("VB6")]
        [InlineData("Assembly language")]
        [InlineData("T-SQL")]
        [InlineData("Objective-C")]
        public async Task PutAddsSkillWithNonAlphabetCharactersTest(string categoryName)
        {
            // See https://github.com/Divergic/techmentorapi/issues/22
            var expected = Model.Using<ProfileBuildStrategy>().Create<UpdatableProfile>()
                .Set(x => x.Skills.First().Name = categoryName);
            var user = ClaimsIdentityFactory.Build(null, expected);

            await Client.Put(ApiLocation.AccountProfile, _logger, expected, user, HttpStatusCode.NoContent)
                .ConfigureAwait(false);

            var actual = await Client.Get<Profile>(ApiLocation.AccountProfile, _logger, user).ConfigureAwait(false);

            actual.ShouldBeEquivalentTo(expected, opt => opt.ExcludingMissingMembers());
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

            await Client.Put(ApiLocation.AccountProfile, _logger, expected, user, HttpStatusCode.NoContent)
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task PutCreatesQueueMessageForNewCategoryTest()
        {
            var categoryToKeep = Model.Using<ProfileBuildStrategy>().Create<Skill>();
            var profile = Model.Using<ProfileBuildStrategy>().Create<UpdatableProfile>().Set(
                x =>
                {
                    x.Gender = null;
                    x.Skills.Clear();
                    x.Languages.Clear();

                    x.Skills.Add(categoryToKeep);
                });
            var user = ClaimsIdentityFactory.Build(null, profile);

            await Client.Put(ApiLocation.AccountProfile, _logger, profile, user, HttpStatusCode.NoContent)
                .ConfigureAwait(false);

            const string queueName = "newcategories";
            var expected = CategoryGroup.Skill + Environment.NewLine + categoryToKeep.Name;

            var storageAccount = CloudStorageAccount.Parse(Config.Storage.ConnectionString);
            var client = storageAccount.CreateCloudQueueClient();
            var queue = client.GetQueueReference(queueName);

            var queueItem = await queue.GetMessageAsync().ConfigureAwait(false);

            while (queueItem != null)
            {
                var actual = queueItem.AsString;

                if (actual == expected)
                {
                    // We found the item
                    return;
                }

                // Check the next queue item
                queueItem = await queue.GetMessageAsync().ConfigureAwait(false);
            }

            throw new InvalidOperationException("Expected queue item was not found.");
        }

        [Fact]
        public async Task PutCreatesQueueMessageForUpdatedProfileTest()
        {
            var categoryToKeep = Model.Using<ProfileBuildStrategy>().Create<Skill>();
            var profile = Model.Using<ProfileBuildStrategy>().Create<UpdatableProfile>().Set(
                x =>
                {
                    x.Gender = null;
                    x.Skills.Clear();
                    x.Languages.Clear();
                    x.About = Guid.NewGuid().ToString();

                    x.Skills.Add(categoryToKeep);
                });
            var user = ClaimsIdentityFactory.Build(null, profile);

            await Client.Put(ApiLocation.AccountProfile, _logger, profile, user, HttpStatusCode.NoContent)
                .ConfigureAwait(false);

            const string queueName = "updatedprofiles";

            var storageAccount = CloudStorageAccount.Parse(Config.Storage.ConnectionString);
            var client = storageAccount.CreateCloudQueueClient();
            var queue = client.GetQueueReference(queueName);

            var queueItem = await queue.GetMessageAsync().ConfigureAwait(false);

            while (queueItem != null)
            {
                var messageContent = queueItem.AsString;

                if (messageContent.Contains(profile.About))
                {
                    var actual = JsonConvert.DeserializeObject<UpdatableProfile>(messageContent);

                    actual.ShouldBeEquivalentTo(profile);

                    return;
                }

                // Check the next queue item
                queueItem = await queue.GetMessageAsync().ConfigureAwait(false);
            }

            throw new InvalidOperationException("Expected queue item was not found.");
        }

        [Fact]
        public async Task PutDoesNotAllowOverPostingTheBannedAtValueTest()
        {
            var account = Model.Create<Account>();
            var profile = await Model.Using<ProfileBuildStrategy>().Create<Profile>()
                .Set(x => x.BannedAt = DateTimeOffset.UtcNow).ClearCategories().Save(_logger, account)
                .ConfigureAwait(false);
            var user = ClaimsIdentityFactory.Build(account, profile);

            profile.BannedAt = null;

            // Attempt to overpost BannedAt to clear it
            await Client.Put(ApiLocation.AccountProfile, _logger, profile, user, HttpStatusCode.NoContent)
                .ConfigureAwait(false);

            var actual = await Client.Get<Profile>(ApiLocation.AccountProfile, _logger, user).ConfigureAwait(false);

            actual.BannedAt.Should().HaveValue();
        }

        [Fact]
        public async Task PutDoesNotCauseNewCategoryToBePublicallyVisibleTest()
        {
            var expected = Model.Using<ProfileBuildStrategy>().Create<UpdatableProfile>();
            var skill = Model.Using<ProfileBuildStrategy>().Create<Skill>();
            var user = ClaimsIdentityFactory.Build(null, expected);

            expected.Skills.Clear();
            expected.Skills.Add(skill);

            await Client.Put(ApiLocation.AccountProfile, _logger, expected, user, HttpStatusCode.NoContent)
                .ConfigureAwait(false);

            var actual = await Client.Get<List<PublicCategory>>(ApiLocation.Categories, _logger).ConfigureAwait(false);

            actual.Should().NotContain(
                x => x.Group == CategoryGroup.Skill && x.Name.Equals(skill.Name, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task PutReturnsBadRequestForEmptyBodyTest()
        {
            var user = ClaimsIdentityFactory.Build();

            await Client.Put(ApiLocation.AccountProfile, _logger, null, user, HttpStatusCode.BadRequest)
                .ConfigureAwait(false);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("stuff")]
        public async Task PutReturnsBadRequestForInvalidEmailTest(string email)
        {
            var expected = Model.Using<ProfileBuildStrategy>().Create<UpdatableProfile>().Set(x => x.Email = email);
            var user = ClaimsIdentityFactory.Build(null, expected);

            await Client.Put(ApiLocation.AccountProfile, _logger, expected, user, HttpStatusCode.BadRequest)
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

            await Client.Put(ApiLocation.AccountProfile, _logger, expected, user, HttpStatusCode.BadRequest)
                .ConfigureAwait(false);
        }

        [Theory]
        [InlineData("some\\value")]
        [InlineData("some/value")]
        [InlineData("\\somevalue")]
        [InlineData("/somevalue")]
        [InlineData("somevalue\\")]
        [InlineData("somevalue/")]
        public async Task PutReturnsBadRequestForInvalidGenderTest(string gender)
        {
            var expected = Model.Using<ProfileBuildStrategy>().Create<UpdatableProfile>().Set(x => x.Gender = gender);

            var user = ClaimsIdentityFactory.Build(null, expected);

            await Client.Put(ApiLocation.AccountProfile, _logger, expected, user, HttpStatusCode.BadRequest)
                .ConfigureAwait(false);
        }

        [Theory]
        [InlineData("some\\value")]
        [InlineData("some/value")]
        [InlineData("\\somevalue")]
        [InlineData("/somevalue")]
        [InlineData("somevalue\\")]
        [InlineData("somevalue/")]
        public async Task PutReturnsBadRequestForInvalidLanguageTest(string language)
        {
            var expected = Model.Using<ProfileBuildStrategy>().Create<UpdatableProfile>()
                .Set(x => x.Languages.Add(language));

            var user = ClaimsIdentityFactory.Build(null, expected);

            await Client.Put(ApiLocation.AccountProfile, _logger, expected, user, HttpStatusCode.BadRequest)
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

            await Client.Put(ApiLocation.AccountProfile, _logger, expected, user, HttpStatusCode.BadRequest)
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task PutReturnsBadRequestForInvalidProfileStatusTest()
        {
            var expected = Model.Using<ProfileBuildStrategy>().Create<UpdatableProfile>()
                .Set(x => x.Status = (ProfileStatus)int.MaxValue);
            var user = ClaimsIdentityFactory.Build(null, expected);

            await Client.Put(ApiLocation.AccountProfile, _logger, expected, user, HttpStatusCode.BadRequest)
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

            await Client.Put(ApiLocation.AccountProfile, _logger, expected, user, HttpStatusCode.BadRequest)
                .ConfigureAwait(false);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("some\\value")]
        [InlineData("some/value")]
        [InlineData("\\somevalue")]
        [InlineData("/somevalue")]
        [InlineData("somevalue\\")]
        [InlineData("somevalue/")]
        public async Task PutReturnsBadRequestForInvalidSkillNameTest(string name)
        {
            var expected = Model.Using<ProfileBuildStrategy>().Create<UpdatableProfile>();
            var skill = Model.Using<ProfileBuildStrategy>().Create<Skill>().Set(x => x.Name = name);

            expected.Skills.Add(skill);

            var user = ClaimsIdentityFactory.Build(null, expected);

            await Client.Put(ApiLocation.AccountProfile, _logger, expected, user, HttpStatusCode.BadRequest)
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

            await Client.Put(ApiLocation.AccountProfile, _logger, expected, user, HttpStatusCode.BadRequest)
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

            await Client.Put(ApiLocation.AccountProfile, _logger, expected, user, HttpStatusCode.BadRequest)
                .ConfigureAwait(false);
        }

        [Theory]
        [MemberData(nameof(InvalidYearDataSource))]
        public async Task PutReturnsBadRequestForInvalidYearStartedInTechTest(int year)
        {
            var expected = Model.Using<ProfileBuildStrategy>().Create<UpdatableProfile>()
                .Set(x => x.YearStartedInTech = year);
            var user = ClaimsIdentityFactory.Build(null, expected);

            await Client.Put(ApiLocation.AccountProfile, _logger, expected, user, HttpStatusCode.BadRequest)
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

            await Client.Put(ApiLocation.AccountProfile, _logger, expected, user, HttpStatusCode.BadRequest)
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task PutReturnsBadRequestWhenAcceptCoCIsFalseTest()
        {
            var expected = Model.Using<ProfileBuildStrategy>().Create<UpdatableProfile>().Set(x => x.AcceptCoC = false);
            var user = ClaimsIdentityFactory.Build(null, expected);

            await Client.Put(ApiLocation.AccountProfile, _logger, expected, user, HttpStatusCode.BadRequest)
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task PutReturnsUnauthorizedForAnonymousUserTest()
        {
            var profile = Model.Using<ProfileBuildStrategy>().Create<Profile>();

            await Client.Put(ApiLocation.AccountProfile, _logger, profile, null, HttpStatusCode.Unauthorized)
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task PutUpdatesProfileInformationForBannedAccountTest()
        {
            var expected = Model.Using<ProfileBuildStrategy>().Create<UpdatableProfile>();
            var user = ClaimsIdentityFactory.Build(null, expected);

            // Create the profile
            await Client.Put(ApiLocation.AccountProfile, _logger, expected, user, HttpStatusCode.NoContent)
                .ConfigureAwait(false);

            // Get the profile
            var storedProfile =
                await Client.Get<Profile>(ApiLocation.AccountProfile, _logger, user).ConfigureAwait(false);

            var administrator = ClaimsIdentityFactory.Build().AsAdministrator();

            // Ban the profile
            await Client.Delete(ApiLocation.ProfileFor(storedProfile.Id), _logger, administrator).ConfigureAwait(false);

            // Make a change to the profile
            expected.FirstName = Guid.NewGuid().ToString();

            // Update the profile again
            await Client.Put(ApiLocation.AccountProfile, _logger, expected, user, HttpStatusCode.NoContent)
                .ConfigureAwait(false);

            var actual = await Client.Get<Profile>(ApiLocation.AccountProfile, _logger, user).ConfigureAwait(false);

            actual.ShouldBeEquivalentTo(expected, opt => opt.ExcludingMissingMembers());
        }

        [Fact]
        public async Task PutUpdatesProfileInformationTest()
        {
            var expected = Model.Using<ProfileBuildStrategy>().Create<UpdatableProfile>();
            var user = ClaimsIdentityFactory.Build(null, expected);

            await Client.Put(ApiLocation.AccountProfile, _logger, expected, user, HttpStatusCode.NoContent)
                .ConfigureAwait(false);

            var actual = await Client.Get<Profile>(ApiLocation.AccountProfile, _logger, user).ConfigureAwait(false);

            actual.ShouldBeEquivalentTo(expected, opt => opt.ExcludingMissingMembers());
        }

        [Theory]
        [InlineData("梅", "張")]
        [InlineData("Françoise", "Gagné")]
        public async Task PutUpdatesProfileWithNonAsciiCharactersTest(string firstName, string lastName)
        {
            var expected = Model.Using<ProfileBuildStrategy>().Create<UpdatableProfile>()
                .Set(x => x.FirstName = firstName).Set(x => x.LastName = lastName);
            var user = ClaimsIdentityFactory.Build(null, expected);

            await Client.Put(ApiLocation.AccountProfile, _logger, expected, user, HttpStatusCode.NoContent)
                .ConfigureAwait(false);

            var actual = await Client.Get<Profile>(ApiLocation.AccountProfile, _logger, user).ConfigureAwait(false);

            actual.ShouldBeEquivalentTo(expected, opt => opt.ExcludingMissingMembers());
        }
    }
}