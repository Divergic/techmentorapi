﻿namespace TechMentorApi.Business
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using TechMentorApi.Model;
    using EnsureThat;
    using Microsoft.Extensions.Caching.Memory;

    public class CacheManager : ICacheManager
    {
        private const string CategoriesCacheKey = "Categories";
        private const string ProfileResultsCacheKey = "ProfileResults";
        private readonly IMemoryCache _cache;
        private readonly ICacheConfig _config;

        public CacheManager(IMemoryCache cache, ICacheConfig config)
        {
            Ensure.Any.IsNotNull(cache, nameof(cache));
            Ensure.Any.IsNotNull(config, nameof(config));

            _cache = cache;
            _config = config;
        }

        public Account GetAccount(string username)
        {
            Ensure.String.IsNotNullOrWhiteSpace(username, nameof(username));

            var cacheKey = BuildAccountCacheKey(username);

            var id = _cache.Get<Guid>(cacheKey);

            if (id == Guid.Empty)
            {
                return null;
            }

            var account = new Account(username) {Id = id};

            return account;
        }

        public ICollection<Category> GetCategories()
        {
            return _cache.Get<ICollection<Category>>(CategoriesCacheKey);
        }

        public ICollection<Guid> GetCategoryLinks(ProfileFilter filter)
        {
            Ensure.Any.IsNotNull(filter, nameof(filter));

            var cacheKey = BuildCategoryLinkCacheKey(filter);

            return _cache.Get<ICollection<Guid>>(cacheKey);
        }

        public Profile GetProfile(Guid id)
        {
            Ensure.Guid.IsNotEmpty(id, nameof(id));

            var cacheKey = BuildProfileCacheKey(id);

            return _cache.Get<Profile>(cacheKey);
        }

        public ICollection<ProfileResult> GetProfileResults()
        {
            return _cache.Get<ICollection<ProfileResult>>(ProfileResultsCacheKey);
        }

        public void RemoveCategories()
        {
            _cache.Remove(CategoriesCacheKey);
        }

        public void RemoveCategoryLinks(ProfileFilter filter)
        {
            Ensure.Any.IsNotNull(filter, nameof(filter));

            var cacheKey = BuildCategoryLinkCacheKey(filter);

            _cache.Remove(cacheKey);
        }

        public void RemoveProfile(Guid id)
        {
            Ensure.Guid.IsNotEmpty(id, nameof(id));

            var cacheKey = BuildProfileCacheKey(id);

            _cache.Remove(cacheKey);
        }

        public void StoreAccount(Account account)
        {
            Ensure.Any.IsNotNull(account, nameof(account));

            var cacheKey = BuildAccountCacheKey(account.Username);

            var options = new MemoryCacheEntryOptions {SlidingExpiration = _config.AccountExpiration};

            _cache.Set(cacheKey, account.Id, options);
        }

        public void StoreCategories(ICollection<Category> categories)
        {
            Ensure.Any.IsNotNull(categories, nameof(categories));

            var options = new MemoryCacheEntryOptions {SlidingExpiration = _config.CategoriesExpiration};

            _cache.Set(CategoriesCacheKey, categories, options);
        }

        public void StoreCategoryLinks(ProfileFilter filter, ICollection<Guid> links)
        {
            Ensure.Any.IsNotNull(filter, nameof(filter));
            Ensure.Any.IsNotNull(links, nameof(links));

            var cacheKey = BuildCategoryLinkCacheKey(filter);

            var options = new MemoryCacheEntryOptions {SlidingExpiration = _config.CategoryLinksExpiration};

            _cache.Set(cacheKey, links, options);
        }

        public void StoreProfile(Profile profile)
        {
            Ensure.Any.IsNotNull(profile, nameof(profile));

            var cacheKey = BuildProfileCacheKey(profile.Id);

            var options = new MemoryCacheEntryOptions {SlidingExpiration = _config.ProfileExpiration};

            _cache.Set(cacheKey, profile, options);
        }

        public void StoreProfileResults(ICollection<ProfileResult> results)
        {
            Ensure.Any.IsNotNull(results, nameof(results));

            var options = new MemoryCacheEntryOptions
            {
                SlidingExpiration = _config.ProfileResultsExpiration
            };

            _cache.Set(ProfileResultsCacheKey, results, options);
        }

        private static string BuildAccountCacheKey(string username)
        {
            // The cache key has a prefix to partition this type of object just in case there is a key collision with another object type
            return "Account|" + username;
        }

        private static string BuildCategoryLinkCacheKey(ProfileFilter filter)
        {
            Debug.Assert(filter != null);

            // The cache key has a prefix to partition this type of object just in case there is a key collision with another object type
            return "CategoryLinks|" + filter.CategoryGroup + "|" + filter.CategoryName;
        }

        private static string BuildProfileCacheKey(Guid id)
        {
            // The cache key has a prefix to partition this type of object just in case there is a key collision with another object type
            return "Profile|" + id;
        }
    }
}