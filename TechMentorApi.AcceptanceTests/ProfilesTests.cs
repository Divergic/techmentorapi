﻿namespace TechMentorApi.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using TechMentorApi.Model;
    using FluentAssertions;
    using Microsoft.Extensions.Logging;
    using ModelBuilder;
    using Xunit;
    using Xunit.Abstractions;

    public class ProfilesTests
    {
        private readonly ILogger<ProfilesTests> _logger;
        private readonly ITestOutputHelper _output;

        public ProfilesTests(ITestOutputHelper output)
        {
            _output = output;
            _logger = output.BuildLoggerFor<ProfilesTests>();
        }

        [Fact]
        public async Task GetDoesNotReturnBannedProfileTest()
        {
            var profile = await Model.Using<ProfileBuildStrategy>().Create<Profile>()
                .Set(x => x.BannedAt = DateTimeOffset.UtcNow).Save().ConfigureAwait(false);

            var actual = await Client.Get<List<ProfileResult>>(ApiLocation.Profiles, _logger).ConfigureAwait(false);

            actual.Should().NotContain(x => x.Id == profile.Id);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("Male")]
        public async Task GetDoesNotReturnProfileAfterGenderUpdatedTest(string newGender)
        {
            var account = Model.Create<Account>();
            var profile = await Model.Using<ProfileBuildStrategy>().Create<Profile>().Set(x => x.Gender = "Female")
                .Save(_logger, account).ConfigureAwait(false);
            var filters = new List<ProfileFilter>
            {
                new ProfileFilter
                {
                    CategoryGroup = CategoryGroup.Gender,
                    CategoryName = profile.Gender
                }
            };
            var address = ApiLocation.ProfilesMatching(filters);

            var firstActual = await Client.Get<List<ProfileResult>>(address, _logger).ConfigureAwait(false);

            firstActual.Single(x => x.Id == profile.Id)
                .ShouldBeEquivalentTo(profile, opt => opt.ExcludingMissingMembers());

            await profile.Set(x => x.Gender = newGender).Save(_logger, account).ConfigureAwait(false);

            var secondActual = await Client.Get<List<ProfileResult>>(address, _logger).ConfigureAwait(false);

            secondActual.Should().NotContain(x => x.Id == profile.Id);
        }

        [Fact]
        public async Task GetDoesNotReturnProfileAfterLanguageRemovedTest()
        {
            var account = Model.Create<Account>();
            var profile = await Model.Using<ProfileBuildStrategy>().Create<Profile>().Save(_logger, account)
                .ConfigureAwait(false);
            var languageToRemoved = profile.Languages.First();
            var filters = new List<ProfileFilter>
            {
                new ProfileFilter
                {
                    CategoryGroup = CategoryGroup.Language,
                    CategoryName = languageToRemoved
                }
            };
            var address = ApiLocation.ProfilesMatching(filters);

            var firstActual = await Client.Get<List<ProfileResult>>(address, _logger).ConfigureAwait(false);

            firstActual.Single(x => x.Id == profile.Id)
                .ShouldBeEquivalentTo(profile, opt => opt.ExcludingMissingMembers());

            await profile.Set(x => x.Languages.Remove(languageToRemoved)).Save(_logger, account).ConfigureAwait(false);

            var secondActual = await Client.Get<List<ProfileResult>>(address, _logger).ConfigureAwait(false);

            secondActual.Should().NotContain(x => x.Id == profile.Id);
        }

        [Fact]
        public async Task GetDoesNotReturnProfileAfterProfileBannedWhenPreviouslyMatchedFilterTest()
        {
            var account = Model.Create<Account>();
            var profile = await Model.Using<ProfileBuildStrategy>().Create<Profile>().Save(_logger, account)
                .ConfigureAwait(false);
            var filters = new List<ProfileFilter>
            {
                new ProfileFilter
                {
                    CategoryGroup = CategoryGroup.Language,
                    CategoryName = profile.Languages.First()
                }
            };
            var address = ApiLocation.ProfilesMatching(filters);

            var firstActual = await Client.Get<List<ProfileResult>>(address, _logger).ConfigureAwait(false);

            firstActual.Single(x => x.Id == profile.Id)
                .ShouldBeEquivalentTo(profile, opt => opt.ExcludingMissingMembers());

            await profile.Set(x => x.BannedAt = DateTimeOffset.UtcNow).Save(_logger, account).ConfigureAwait(false);

            var secondActual = await Client.Get<List<ProfileResult>>(address, _logger).ConfigureAwait(false);

            secondActual.Should().NotContain(x => x.Id == profile.Id);
        }

        [Fact]
        public async Task GetDoesNotReturnProfileAfterProfileHiddenTest()
        {
            var account = Model.Create<Account>();
            var profile = await Model.Using<ProfileBuildStrategy>().Create<Profile>().Save(_logger, account)
                .ConfigureAwait(false);
            var filters = new List<ProfileFilter>
            {
                new ProfileFilter
                {
                    CategoryGroup = CategoryGroup.Language,
                    CategoryName = profile.Languages.First()
                }
            };
            var address = ApiLocation.ProfilesMatching(filters);

            var firstActual = await Client.Get<List<ProfileResult>>(address, _logger).ConfigureAwait(false);

            firstActual.Single(x => x.Id == profile.Id)
                .ShouldBeEquivalentTo(profile, opt => opt.ExcludingMissingMembers());

            await profile.Set(x => x.Status = ProfileStatus.Hidden).Save(_logger, account).ConfigureAwait(false);

            var secondActual = await Client.Get<List<ProfileResult>>(address, _logger).ConfigureAwait(false);

            secondActual.Should().NotContain(x => x.Id == profile.Id);
        }

        [Theory]
        [InlineData(ProfileStatus.Available)]
        [InlineData(ProfileStatus.Unavailable)]
        public async Task GetDoesNotReturnProfileWhenBannedTest(ProfileStatus status)
        {
            var account = Model.Create<Account>();
            var profile = await Model.Using<ProfileBuildStrategy>().Create<Profile>().Set(x => x.Status = status)
                .Save(_logger, account).ConfigureAwait(false);

            var firstActual = await Client.Get<List<ProfileResult>>(ApiLocation.Profiles, _logger)
                .ConfigureAwait(false);

            firstActual.Should().Contain(x => x.Id == profile.Id);

            await profile.Set(x => x.BannedAt = DateTimeOffset.UtcNow).Save(_logger, account).ConfigureAwait(false);

            var secondActual = await Client.Get<List<ProfileResult>>(ApiLocation.Profiles, _logger)
                .ConfigureAwait(false);

            secondActual.Should().NotContain(x => x.Id == profile.Id);
        }

        [Theory]
        [InlineData(ProfileStatus.Available)]
        [InlineData(ProfileStatus.Unavailable)]
        public async Task GetDoesNotReturnProfileWhenUpdatedToHiddenTest(ProfileStatus status)
        {
            var account = Model.Create<Account>();
            var profile = await Model.Using<ProfileBuildStrategy>().Create<Profile>().Set(x => x.Status = status)
                .Save(_logger, account).ConfigureAwait(false);

            var firstActual = await Client.Get<List<ProfileResult>>(ApiLocation.Profiles, _logger)
                .ConfigureAwait(false);

            firstActual.Should().Contain(x => x.Id == profile.Id);

            await profile.Set(x => x.Status = ProfileStatus.Hidden).Save(_logger, account).ConfigureAwait(false);

            var secondActual = await Client.Get<List<ProfileResult>>(ApiLocation.Profiles, _logger)
                .ConfigureAwait(false);

            secondActual.Should().NotContain(x => x.Id == profile.Id);
        }

        [Fact]
        public async Task GetDoesReturnProfileAfterGenderUpdatedMatchesExistingFilterTest()
        {
            var account = Model.Create<Account>();
            var profile = await Model.Using<ProfileBuildStrategy>().Create<Profile>().Save(_logger, account)
                .ConfigureAwait(false);
            var newGender = Guid.NewGuid().ToString();
            var filters = new List<ProfileFilter>
            {
                new ProfileFilter
                {
                    CategoryGroup = CategoryGroup.Gender,
                    CategoryName = newGender
                }
            };
            var address = ApiLocation.ProfilesMatching(filters);

            var firstActual = await Client.Get<List<ProfileResult>>(address, _logger).ConfigureAwait(false);

            firstActual.Should().NotContain(x => x.Id == profile.Id);

            await profile.Set(x => x.Gender = newGender).Save(_logger, account).ConfigureAwait(false);

            var secondActual = await Client.Get<List<ProfileResult>>(address, _logger).ConfigureAwait(false);

            secondActual.Single(x => x.Id == profile.Id)
                .ShouldBeEquivalentTo(profile, opt => opt.ExcludingMissingMembers());
        }

        [Fact]
        public async Task GetIgnoresUnsupportedFiltersTest()
        {
            var account = Model.Create<Account>();
            var profile = await Model.Using<ProfileBuildStrategy>().Create<Profile>().Save(_logger, account)
                .ConfigureAwait(false);

            var firstActual = await Client.Get<List<ProfileResult>>(ApiLocation.Profiles, _logger)
                .ConfigureAwait(false);

            firstActual.Single(x => x.Id == profile.Id)
                .ShouldBeEquivalentTo(profile, opt => opt.ExcludingMissingMembers());

            var address = new Uri(ApiLocation.Profiles + "?unknown=filter");
            var secondActual = await Client.Get<List<ProfileResult>>(address, _logger).ConfigureAwait(false);

            secondActual.ShouldAllBeEquivalentTo(firstActual);
        }

        [Fact]
        public async Task GetReturnProfileAfterLanguageAddedToMatchExistingFilterTest()
        {
            var account = Model.Create<Account>();
            var newLanguage = Guid.NewGuid().ToString();
            var profile = await Model.Using<ProfileBuildStrategy>().Create<Profile>().Save(_logger, account)
                .ConfigureAwait(false);
            var filters = new List<ProfileFilter>
            {
                new ProfileFilter
                {
                    CategoryGroup = CategoryGroup.Language,
                    CategoryName = newLanguage
                }
            };
            var address = ApiLocation.ProfilesMatching(filters);

            var firstActual = await Client.Get<List<ProfileResult>>(address, _logger).ConfigureAwait(false);

            firstActual.Should().NotContain(x => x.Id == profile.Id);

            await profile.Set(x => x.Languages.Add(newLanguage)).Save(_logger, account).ConfigureAwait(false);

            var secondActual = await Client.Get<List<ProfileResult>>(address, _logger).ConfigureAwait(false);

            secondActual.Single(x => x.Id == profile.Id)
                .ShouldBeEquivalentTo(profile, opt => opt.ExcludingMissingMembers());
        }

        [Fact]
        public async Task GetReturnProfileAfterSkillAddedToMatchExistingFilterTest()
        {
            var account = Model.Create<Account>();
            var newSkill = Model.Using<ProfileBuildStrategy>().Create<Skill>();
            var profile = await Model.Using<ProfileBuildStrategy>().Create<Profile>().Save(_logger, account)
                .ConfigureAwait(false);
            var filters = new List<ProfileFilter>
            {
                new ProfileFilter
                {
                    CategoryGroup = CategoryGroup.Skill,
                    CategoryName = newSkill.Name
                }
            };
            var address = ApiLocation.ProfilesMatching(filters);

            var firstActual = await Client.Get<List<ProfileResult>>(address, _logger).ConfigureAwait(false);

            firstActual.Should().NotContain(x => x.Id == profile.Id);

            await profile.Set(x => x.Skills.Add(newSkill)).Save(_logger, account).ConfigureAwait(false);

            var secondActual = await Client.Get<List<ProfileResult>>(address, _logger).ConfigureAwait(false);

            secondActual.Single(x => x.Id == profile.Id)
                .ShouldBeEquivalentTo(profile, opt => opt.ExcludingMissingMembers());
        }

        [Theory]
        [InlineData(CategoryGroup.Gender)]
        [InlineData(CategoryGroup.Language)]
        [InlineData(CategoryGroup.Skill)]
        public async Task GetReturnsEmptyWhenNoProfilesMatchFilterTest(CategoryGroup group)
        {
            // Ensure there is at least one profile available
            await Model.Using<ProfileBuildStrategy>().Create<Profile>().Save(_logger).ConfigureAwait(false);

            var filters = new List<ProfileFilter>
            {
                new ProfileFilter
                {
                    CategoryGroup = group,
                    CategoryName = Guid.NewGuid().ToString()
                }
            };
            var address = ApiLocation.ProfilesMatching(filters);

            var actual = await Client.Get<List<ProfileResult>>(address, _logger).ConfigureAwait(false);

            actual.Should().BeEmpty();
        }

        [Fact]
        public async Task GetReturnsMostRecentDataWhenProfileUpdatedTest()
        {
            var account = Model.Create<Account>();
            var profile = await Model.Using<ProfileBuildStrategy>().Create<Profile>().Save(_logger, account)
                .ConfigureAwait(false);

            var firstActual = await Client.Get<List<ProfileResult>>(ApiLocation.Profiles, _logger)
                .ConfigureAwait(false);

            firstActual.Single(x => x.Id == profile.Id)
                .ShouldBeEquivalentTo(profile, opt => opt.ExcludingMissingMembers());

            var template = Model.Using<ProfileBuildStrategy>().Create<ProfileResult>();

            profile.BirthYear = template.BirthYear;
            profile.YearStartedInTech = template.YearStartedInTech;
            profile.FirstName = template.FirstName;
            profile.Gender = template.Gender;
            profile.LastName = template.LastName;
            profile.TimeZone = template.TimeZone;

            await profile.Save(_logger, account).ConfigureAwait(false);

            var secondActual = await Client.Get<List<ProfileResult>>(ApiLocation.Profiles, _logger)
                .ConfigureAwait(false);

            secondActual.Single(x => x.Id == profile.Id)
                .ShouldBeEquivalentTo(profile, opt => opt.ExcludingMissingMembers());
        }

        [Fact]
        public async Task GetReturnsMultipleProfilesMatchingFiltersTest()
        {
            var firstProfile = await Model.Using<ProfileBuildStrategy>().Create<Profile>().Save(_logger)
                .ConfigureAwait(false);
            var secondProfile = await Model.Using<ProfileBuildStrategy>().Create<Profile>()
                .Set(x => x.Gender = firstProfile.Gender).Save(_logger).ConfigureAwait(false);
            var filters = new List<ProfileFilter>
            {
                new ProfileFilter
                {
                    CategoryGroup = CategoryGroup.Gender,
                    CategoryName = firstProfile.Gender
                }
            };
            var address = ApiLocation.ProfilesMatching(filters);

            var actual = await Client.Get<List<ProfileResult>>(address, _logger).ConfigureAwait(false);

            actual.Single(x => x.Id == firstProfile.Id)
                .ShouldBeEquivalentTo(firstProfile, opt => opt.ExcludingMissingMembers());
            actual.Single(x => x.Id == secondProfile.Id)
                .ShouldBeEquivalentTo(secondProfile, opt => opt.ExcludingMissingMembers());
        }

        [Theory]
        [InlineData(ProfileStatus.Hidden, false)]
        [InlineData(ProfileStatus.Available, true)]
        [InlineData(ProfileStatus.Unavailable, true)]
        public async Task GetReturnsProfileBasedOnStatusTest(ProfileStatus status, bool found)
        {
            var profile = await Model.Using<ProfileBuildStrategy>().Create<Profile>().Set(x => x.Status = status).Save()
                .ConfigureAwait(false);

            var actual = await Client.Get<List<ProfileResult>>(ApiLocation.Profiles, _logger).ConfigureAwait(false);

            if (found)
            {
                actual.Should().Contain(x => x.Id == profile.Id);
            }
            else
            {
                actual.Should().NotContain(x => x.Id == profile.Id);
            }
        }

        [Fact]
        public async Task GetReturnsProfileWithAllCategoryFiltersAppliedTest()
        {
            var profile = await Model.Using<ProfileBuildStrategy>().Create<Profile>().Save(_logger)
                .ConfigureAwait(false);
            var filters = new List<ProfileFilter>();

            if (string.IsNullOrWhiteSpace(profile.Gender))
            {
                filters.Add(
                    new ProfileFilter
                    {
                        CategoryGroup = CategoryGroup.Gender,
                        CategoryName = profile.Gender
                    });
            }

            foreach (var language in profile.Languages)
            {
                filters.Add(
                    new ProfileFilter
                    {
                        CategoryGroup = CategoryGroup.Language,
                        CategoryName = language
                    });
            }

            foreach (var skill in profile.Skills)
            {
                filters.Add(
                    new ProfileFilter
                    {
                        CategoryGroup = CategoryGroup.Skill,
                        CategoryName = skill.Name
                    });
            }

            var address = ApiLocation.ProfilesMatching(filters);

            var actual = await Client.Get<List<ProfileResult>>(address, _logger).ConfigureAwait(false);

            actual.Single(x => x.Id == profile.Id).ShouldBeEquivalentTo(profile, opt => opt.ExcludingMissingMembers());
        }

        [Theory]
        [InlineData("Female")]
        [InlineData("female")]
        [InlineData("FEMALE")]
        public async Task GetReturnsProfileWithCaseInsensitiveFilterMatchTest(string filterName)
        {
            var category = new NewCategory
            {
                Group = CategoryGroup.Gender,
                Name = "Female"
            };

            await category.Save(_logger).ConfigureAwait(false);

            var profile = await Model.Using<ProfileBuildStrategy>().Create<Profile>().Set(x => x.Gender = "Female")
                .Save(_logger).ConfigureAwait(false);
            var filters = new List<ProfileFilter>
            {
                new ProfileFilter
                {
                    CategoryGroup = CategoryGroup.Gender,
                    CategoryName = filterName
                }
            };
            var address = ApiLocation.ProfilesMatching(filters);

            var actual = await Client.Get<List<ProfileResult>>(address, _logger).ConfigureAwait(false);

            actual.Single(x => x.Id == profile.Id).ShouldBeEquivalentTo(profile, opt => opt.ExcludingMissingMembers());
        }

        [Fact]
        public async Task GetReturnsProfileWithGenderFilterTest()
        {
            var profile = await Model.Using<ProfileBuildStrategy>().Create<Profile>().Save(_logger)
                .ConfigureAwait(false);
            var filters = new List<ProfileFilter>
            {
                new ProfileFilter
                {
                    CategoryGroup = CategoryGroup.Gender,
                    CategoryName = profile.Gender
                }
            };
            var address = ApiLocation.ProfilesMatching(filters);

            var actual = await Client.Get<List<ProfileResult>>(address, _logger).ConfigureAwait(false);

            actual.Single(x => x.Id == profile.Id).ShouldBeEquivalentTo(profile, opt => opt.ExcludingMissingMembers());
        }

        [Fact]
        public async Task GetReturnsProfileWithLanguageFilterTest()
        {
            var profile = await Model.Using<ProfileBuildStrategy>().Create<Profile>().Save(_logger)
                .ConfigureAwait(false);
            var filters = new List<ProfileFilter>
            {
                new ProfileFilter
                {
                    CategoryGroup = CategoryGroup.Language,
                    CategoryName = profile.Languages.Skip(2).First()
                }
            };
            var address = ApiLocation.ProfilesMatching(filters);

            var actual = await Client.Get<List<ProfileResult>>(address, _logger).ConfigureAwait(false);

            actual.Single(x => x.Id == profile.Id).ShouldBeEquivalentTo(profile, opt => opt.ExcludingMissingMembers());
        }

        [Fact]
        public async Task GetReturnsProfileWithoutAnyCategoryLinksWhenNoFiltersAppliedTest()
        {
            var profile = await Model.Using<ProfileBuildStrategy>().Create<Profile>().Set(
                x =>
                {
                    x.Skills.Clear();
                    x.Languages.Clear();
                    x.Gender = null;
                }).Save(_logger).ConfigureAwait(false);

            var actual = await Client.Get<List<ProfileResult>>(ApiLocation.Profiles, _logger).ConfigureAwait(false);

            actual.Single(x => x.Id == profile.Id).ShouldBeEquivalentTo(profile, opt => opt.ExcludingMissingMembers());
        }

        [Fact]
        public async Task GetReturnsProfileWithSkillFilterTest()
        {
            var profile = await Model.Using<ProfileBuildStrategy>().Create<Profile>().Save(_logger)
                .ConfigureAwait(false);
            var filters = new List<ProfileFilter>
            {
                new ProfileFilter
                {
                    CategoryGroup = CategoryGroup.Skill,
                    CategoryName = profile.Skills.Skip(2).First().Name
                }
            };
            var address = ApiLocation.ProfilesMatching(filters);

            var actual = await Client.Get<List<ProfileResult>>(address, _logger).ConfigureAwait(false);

            actual.Single(x => x.Id == profile.Id).ShouldBeEquivalentTo(profile, opt => opt.ExcludingMissingMembers());
        }

        [Fact]
        public async Task GetReturnsResultsWithExpectedSortOrderTest()
        {
            var source = await Model.Using<ProfileBuildStrategy>().Create<List<Profile>>().SetEach(
                x =>
                {
                    x.Gender = null;
                    x.Languages.Clear();
                    x.Skills.Clear();
                }).Save().ConfigureAwait(false);

            var expected = (from x in source
                orderby x.Status descending, x.YearStartedInTech ?? 0 descending, x.BirthYear ??
                                                                                  DateTimeOffset.UtcNow.Year
                select x.Id).ToList();

            var address = ApiLocation.Profiles;

            var results = await Client.Get<List<ProfileResult>>(address, _logger).ConfigureAwait(false);
            var actual = new List<ProfileResult>(expected.Count);

            foreach (var result in results)
            {
                if (expected.Any(x => x == result.Id))
                {
                    actual.Add(result);
                }
            }

            actual.Select(x => x.Id).Should().ContainInOrder(expected);
        }
    }
}