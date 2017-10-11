﻿namespace TechMentorApi.Azure
{
    using System.Threading;
    using System.Threading.Tasks;
    using TechMentorApi.Model;

    public interface IAccountStore
    {
        Task<AccountResult> GetAccount(string provider, string username, CancellationToken cancellationToken);

        Task RegisterAccount(Account account, CancellationToken cancellationToken);
    }
}