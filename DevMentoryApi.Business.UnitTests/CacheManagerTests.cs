﻿namespace DevMentoryApi.Business.UnitTests
{
    using System;
    using System.Collections.Generic;
    using DevMentorApi.Business;
    using DevMentorApi.Model;
    using FluentAssertions;
    using Microsoft.Extensions.Caching.Memory;
    using ModelBuilder;
    using NSubstitute;
    using Xunit;

    public class CacheManagerTests
    {
        [Fact]
        public void GetAccountReturnsCachedAccountTest()
        {
            var expected = Model.Create<Account>();
            var cacheKey = "Account|" + expected.Username;

            var cache = Substitute.For<IMemoryCache>();
            var config = Substitute.For<ICacheConfig>();

            object value;

            cache.TryGetValue(cacheKey, out value).Returns(
                x =>
                {
                    x[1] = expected;

                    return true;
                });

            var sut = new CacheManager(cache, config);

            var actual = sut.GetAccount(expected.Username);

            actual.ShouldBeEquivalentTo(expected);
        }

        [Fact]
        public void GetAccountReturnsNullWhenCachedAccountNotFoundTest()
        {
            var username = Guid.NewGuid().ToString();
            var cacheKey = "Account|" + username;

            var cache = Substitute.For<IMemoryCache>();
            var config = Substitute.For<ICacheConfig>();

            object value;

            cache.TryGetValue(cacheKey, out value).Returns(x => false);

            var sut = new CacheManager(cache, config);

            var actual = sut.GetAccount(username);

            actual.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void GetAccountThrowsExceptionWithInvalidUsernameTest(string username)
        {
            var cache = Substitute.For<IMemoryCache>();
            var config = Substitute.For<ICacheConfig>();

            var sut = new CacheManager(cache, config);

            Action action = () => sut.GetAccount(username);

            action.ShouldThrow<ArgumentException>();
        }

        [Fact]
        public void GetCategoriesReturnsCachedCategoriesTest()
        {
            var expected = Model.Create<List<Category>>();
            const string CacheKey = "Categories";

            var cache = Substitute.For<IMemoryCache>();
            var config = Substitute.For<ICacheConfig>();

            object value;

            cache.TryGetValue(CacheKey, out value).Returns(
                x =>
                {
                    x[1] = expected;

                    return true;
                });

            var sut = new CacheManager(cache, config);

            var actual = sut.GetCategories();

            actual.ShouldBeEquivalentTo(expected);
        }

        [Fact]
        public void GetCategoriesReturnsNullWhenCachedCategoriesNotFoundTest()
        {
            const string CacheKey = "Categories";

            var cache = Substitute.For<IMemoryCache>();
            var config = Substitute.For<ICacheConfig>();

            object value;

            cache.TryGetValue(CacheKey, out value).Returns(x => false);

            var sut = new CacheManager(cache, config);

            var actual = sut.GetCategories();

            actual.Should().BeNull();
        }

        [Fact]
        public void GetProfileReturnsCachedProfileTest()
        {
            var expected = Model.Create<Profile>();
            var cacheKey = "Profile|" + expected.Id;

            var cache = Substitute.For<IMemoryCache>();
            var config = Substitute.For<ICacheConfig>();

            object value;

            cache.TryGetValue(cacheKey, out value).Returns(
                x =>
                {
                    x[1] = expected;

                    return true;
                });

            var sut = new CacheManager(cache, config);

            var actual = sut.GetProfile(expected.Id);

            actual.ShouldBeEquivalentTo(expected);
        }

        [Fact]
        public void GetProfileReturnsNullWhenCachedProfileNotFoundTest()
        {
            var id = Guid.NewGuid();
            var cacheKey = "Profile|" + id;

            var cache = Substitute.For<IMemoryCache>();
            var config = Substitute.For<ICacheConfig>();

            object value;

            cache.TryGetValue(cacheKey, out value).Returns(x => false);

            var sut = new CacheManager(cache, config);

            var actual = sut.GetProfile(id);

            actual.Should().BeNull();
        }

        [Fact]
        public void GetProfileThrowsExceptionWithInvalidIdTest()
        {
            var id = Guid.Empty;

            var cache = Substitute.For<IMemoryCache>();
            var config = Substitute.For<ICacheConfig>();

            var sut = new CacheManager(cache, config);

            Action action = () => sut.GetProfile(id);

            action.ShouldThrow<ArgumentException>();
        }

        [Fact]
        public void RemoveCategoriesRemovesFromCacheTest()
        {
            var cache = Substitute.For<IMemoryCache>();
            var config = Substitute.For<ICacheConfig>();

            var sut = new CacheManager(cache, config);

            sut.RemoveCategories();

            cache.Received().Remove("Categories");
        }

        [Fact]
        public void StoreAccountThrowsExceptionWithNullAccountTest()
        {
            var cache = Substitute.For<IMemoryCache>();
            var config = Substitute.For<ICacheConfig>();

            var sut = new CacheManager(cache, config);

            Action action = () => sut.StoreAccount(null);

            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void StoreAccountWritesAccountToCacheTest()
        {
            var expected = Model.Create<Account>();
            var cacheExpiry = TimeSpan.FromMinutes(23);
            var cacheKey = "Account|" + expected.Username;

            var cache = Substitute.For<IMemoryCache>();
            var config = Substitute.For<ICacheConfig>();
            var cacheEntry = Substitute.For<ICacheEntry>();

            config.AccountExpiration.Returns(cacheExpiry);
            cache.CreateEntry(cacheKey).Returns(cacheEntry);

            var sut = new CacheManager(cache, config);

            sut.StoreAccount(expected);

            cacheEntry.Value.Should().Be(expected);
            cacheEntry.SlidingExpiration.Should().Be(cacheExpiry);
        }

        [Fact]
        public void StoreCategoriesThrowsExceptionWithNullCategoriesTest()
        {
            var cache = Substitute.For<IMemoryCache>();
            var config = Substitute.For<ICacheConfig>();

            var sut = new CacheManager(cache, config);

            Action action = () => sut.StoreCategories(null);

            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void StoreCategoriesWritesCategoriesToCacheTest()
        {
            var expected = Model.Create<List<Category>>();
            var cacheExpiry = TimeSpan.FromMinutes(23);
            const string CacheKey = "Categories";

            var cache = Substitute.For<IMemoryCache>();
            var config = Substitute.For<ICacheConfig>();
            var cacheEntry = Substitute.For<ICacheEntry>();

            config.CategoriesExpiration.Returns(cacheExpiry);
            cache.CreateEntry(CacheKey).Returns(cacheEntry);

            var sut = new CacheManager(cache, config);

            sut.StoreCategories(expected);

            cacheEntry.Value.Should().Be(expected);
            cacheEntry.SlidingExpiration.Should().Be(cacheExpiry);
        }

        [Fact]
        public void StoreProfileThrowsExceptionWithNullProfileTest()
        {
            var cache = Substitute.For<IMemoryCache>();
            var config = Substitute.For<ICacheConfig>();

            var sut = new CacheManager(cache, config);

            Action action = () => sut.StoreProfile(null);

            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void StoreProfileWritesProfileToCacheTest()
        {
            var expected = Model.Create<Profile>();
            var cacheExpiry = TimeSpan.FromMinutes(23);
            var cacheKey = "Profile|" + expected.Id;

            var cache = Substitute.For<IMemoryCache>();
            var config = Substitute.For<ICacheConfig>();
            var cacheEntry = Substitute.For<ICacheEntry>();

            config.ProfileExpiration.Returns(cacheExpiry);
            cache.CreateEntry(cacheKey).Returns(cacheEntry);

            var sut = new CacheManager(cache, config);

            sut.StoreProfile(expected);

            cacheEntry.Value.Should().Be(expected);
            cacheEntry.SlidingExpiration.Should().Be(cacheExpiry);
        }

        [Fact]
        public void ThrowsExceptionWhenCreatedWithNullCacheTest()
        {
            var config = Substitute.For<ICacheConfig>();

            Action action = () => new CacheManager(null, config);

            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void ThrowsExceptionWhenCreatedWithNullConfigTest()
        {
            var cache = Substitute.For<IMemoryCache>();

            Action action = () => new CacheManager(cache, null);

            action.ShouldThrow<ArgumentNullException>();
        }
    }
}