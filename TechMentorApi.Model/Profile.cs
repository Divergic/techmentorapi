﻿namespace TechMentorApi.Model
{
    using System;
    using System.Collections.Generic;
    using EnsureThat;

    public class Profile : UpdatableProfile
    {
        public Profile()
        {
        }

        public Profile(UpdatableProfile profile)
        {
            Ensure.That(profile, nameof(profile)).IsNotNull();

            About = profile.About;
            AvatarETag = profile.AvatarETag;
            PhotoId = profile.PhotoId;
            BirthYear = profile.BirthYear;
            Email = profile.Email;
            FirstName = profile.FirstName;
            Gender = profile.Gender;
            GitHubUsername = profile.GitHubUsername;
            Languages = new List<string>(profile.Languages);
            LastName = profile.LastName;
            Skills = new List<Skill>(profile.Skills);
            Status = profile.Status;
            TimeZone = profile.TimeZone;
            TwitterUsername = profile.TwitterUsername;
            Website = profile.Website;
            YearStartedInTech = profile.YearStartedInTech;
        }

        public DateTimeOffset? BannedAt { get; set; }

        public Guid Id { get; set; }
    }
}