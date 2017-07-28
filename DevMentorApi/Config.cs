﻿namespace DevMentorApi
{
    using DevMentorApi.Azure;
    using DevMentorApi.Model;

    public class Config
    {
        public AuthenticationConfig Authentication { get; set; }

        public CacheConfig Cache { get; set; }

        public StorageConfiguration Storage { get; set; }
    }
}