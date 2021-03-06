﻿namespace TechMentorApi.Business.Queries
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using TechMentorApi.Model;

    public interface ICategoryQuery
    {
        Task<IEnumerable<Category>> GetCategories(ReadType readType, CancellationToken cancellationToken);

        Task<Category> GetCategory(
            ReadType readType,
            CategoryGroup group,
            string name,
            CancellationToken cancellationToken);
    }
}