﻿namespace TechMentorApi.AcceptanceTests
{
    using TechMentorApi.Model;
    using ModelBuilder;

    public class ProfileBuildStrategy : BuildStrategy
    {
        public ProfileBuildStrategy() : base(CreateBuildStrategy())
        {
        }

        private static IBuildStrategy CreateBuildStrategy()
        {
            var compiler = new DefaultBuildStrategyCompiler();

            compiler.AddExecuteOrderRule<Skill>(x => x.YearStarted, 30000);
            compiler.AddExecuteOrderRule<Skill>(x => x.YearLastUsed, 20000);
            compiler.AddCreationRule<UpdatableProfile>(x => x.AcceptCoC, 5000, true);
            compiler.AddCreationRule<UpdatableProfile>(x => x.AcceptTaC, 5000, true);
            compiler.AddCreationRule<UpdatableProfile>(x => x.Status, 5000, ProfileStatus.Available);
            compiler.AddCreationRule<Profile>(x => x.AcceptCoC, 5000, true);
            compiler.AddCreationRule<Profile>(x => x.AcceptTaC, 5000, true);
            compiler.AddCreationRule<Profile>(x => x.BannedAt, 5000, null);
            compiler.AddCreationRule<Profile>(x => x.AcceptedCoCAt, 5000, null);
            compiler.AddCreationRule<Profile>(x => x.AcceptedTaCAt, 5000, null);
            compiler.AddCreationRule<Profile>(x => x.Status, 5000, ProfileStatus.Available);

            compiler.AddValueGenerator<BirthYearValueGenerator>();
            compiler.AddValueGenerator<YearInPastValueGenerator>();

            return compiler.Compile();
        }
    }
}