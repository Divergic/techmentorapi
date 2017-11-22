﻿namespace TechMentorApi.Business.Queries
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using EnsureThat;
    using TechMentorApi.Azure;
    using TechMentorApi.Model;

    public class ProfileSearchQuery : IProfileSearchQuery
    {
        private readonly ICacheManager _cache;
        private readonly ICategoryLinkStore _linkStore;
        private readonly IProfileStore _profileStore;
        private readonly ICategoryQuery _query;

        public ProfileSearchQuery(IProfileStore profileStore, ICategoryLinkStore linkStore, ICacheManager cache,
            ICategoryQuery query)
        {
            Ensure.That(profileStore, nameof(profileStore)).IsNotNull();
            Ensure.That(linkStore, nameof(linkStore)).IsNotNull();
            Ensure.That(cache, nameof(cache)).IsNotNull();
            Ensure.That(query, nameof(query)).IsNotNull();

            _profileStore = profileStore;
            _linkStore = linkStore;
            _cache = cache;
            _query = query;
        }

        public async Task<IEnumerable<ProfileResult>> GetProfileResults(
            IEnumerable<ProfileFilter> filters,
            CancellationToken cancellationToken)
        {
            var matchingProfiles = await FilterResults(filters, cancellationToken).ConfigureAwait(false);

            var cleanedProfiles =
                await RemoveUnapprovedGenders(matchingProfiles, cancellationToken).ConfigureAwait(false);

            return cleanedProfiles;
        }

        private async Task<IEnumerable<ProfileResult>> FilterResults(IEnumerable<ProfileFilter> filters, CancellationToken cancellationToken)
        {
            var results = await LoadResults(cancellationToken).ConfigureAwait(false);

            if (filters == null)
            {
                return results;
            }

            var filterSet = filters.ToList();

            if (filterSet.Count == 0)
            {
                return results;
            }

            // Load the category links for each filter
            var matchingProfileIds = await FilterProfiles(filterSet, cancellationToken).ConfigureAwait(false);

            var matchingProfiles = from x in results
                join y in matchingProfileIds on x.Id equals y
                select x;

            return matchingProfiles;
        }

        private async Task<IEnumerable<Guid>> FilterProfiles(
            ICollection<ProfileFilter> filters,
            CancellationToken cancellationToken)
        {
            var tasks = new List<Task<ICollection<Guid>>>();

            foreach (var filter in filters)
            {
                var task = GetCategoryLinks(filter, cancellationToken);

                tasks.Add(task);
            }

            // ToList is used several times in this method do avoid issues with multiple enumerations of IEnumerable<T> 
            // which can cause a re-execution of the code that produces the data
            var profileLinkTasks = await Task.WhenAll(tasks).ConfigureAwait(false);

            // We will ignore category filters that have no links to profiles
            // This is probably because of stale cache data presented to the website where it looked like categories had profiles when they don't
            // In this scenario we won't apply those filters by ignoring them
            var profileLinksWithContent = profileLinkTasks.Where(x => x.Count > 0).ToList();

            if (profileLinksWithContent.Count == 0)
            {
                // One or many filters have been selected but none of them came back with links to profiles
                // There is no data set we can return therefore the search results will be empty
                return new List<Guid>();
            }

            if (profileLinksWithContent.Count == 1)
            {
                // There is only one group of category links so we don't have to find AND matches to other filters
                return profileLinksWithContent[0];
            }

            // Find the combination of Guids that exist in all the category link lists
            // First start by matching the first to lists of category links
            var matches = profileLinksWithContent[0].Intersect(profileLinksWithContent[1]).ToList();

            if (matches.Count == 0)
            {
                // There are no matches on the first two categories so no need to compare against any other lists of links
                return new List<Guid>();
            }

            // Loop from the third item in the list because we have already compared the first two
            for (var index = 2; index < profileLinksWithContent.Count; index++)
            {
                var nextSetOfLinks = profileLinksWithContent[index];

                // Find the combination of Guids that exist in both previous matches found and the next link lists
                matches = matches.Intersect(nextSetOfLinks).ToList();

                if (matches.Count == 0)
                {
                    // There are no matches on the previous matches and the next one so no need to compare against any other lists of links
                    return new List<Guid>();
                }
            }

            // Profile ids really should be unique but we want to ensure the results here are unique just in case
            return matches.Distinct();
        }

        private async Task<ICollection<Guid>> GetCategoryLinks(
            ProfileFilter filter,
            CancellationToken cancellationToken)
        {
            var cachedLinks = _cache.GetCategoryLinks(filter);

            if (cachedLinks != null)
            {
                return cachedLinks;
            }

            var storedLinks = (await _linkStore
                    .GetCategoryLinks(filter.CategoryGroup, filter.CategoryName, cancellationToken)
                    .ConfigureAwait(false))
                .ToList();

            var links = (from x in storedLinks
                select x.ProfileId).ToList();

            _cache.StoreCategoryLinks(filter, links);

            return links;
        }

        private async Task<IEnumerable<ProfileResult>> LoadResults(CancellationToken cancellationToken)
        {
            var cachedResults = _cache.GetProfileResults();

            if (cachedResults != null)
            {
                return cachedResults;
            }

            var storeResults = (await _profileStore.GetProfileResults(cancellationToken).ConfigureAwait(false))
                .ToList();

            _cache.StoreProfileResults(storeResults);

            return storeResults;
        }

        private async Task<IEnumerable<ProfileResult>> RemoveUnapprovedGenders(
            IEnumerable<ProfileResult> matchingProfiles, CancellationToken cancellationToken)
        {
            var categories = await _query.GetCategories(ReadType.VisibleOnly, cancellationToken).ConfigureAwait(false);
            var approvedGenders = (from x in categories
                where x.Group == CategoryGroup.Gender
                select x.Name.ToUpperInvariant()).ToList();

            var profiles = matchingProfiles.ToList();

            foreach (var profile in profiles)
            {
                if (profile.Gender == null)
                {
                    continue;
                }

                var genderToCheck = profile.Gender.ToUpperInvariant();

                if (approvedGenders.Contains(genderToCheck))
                {
                    continue;
                }

                // The gender in this profile is not approved so we need to clear it
                profile.Gender = null;
            }

            return profiles;
        }
    }
}