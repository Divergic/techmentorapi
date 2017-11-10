﻿namespace TechMentorApi.Controllers
{
    using System;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using EnsureThat;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Swashbuckle.AspNetCore.SwaggerGen;
    using TechMentorApi.Business.Commands;
    using TechMentorApi.Core;
    using TechMentorApi.Model;
    using TechMentorApi.Properties;
    using TechMentorApi.Security;

    public class AvatarsController : Controller
    {
        private readonly IAvatarCommand _command;

        public AvatarsController(IAvatarCommand command)
        {
            Ensure.That(command, nameof(command)).IsNotNull();

            _command = command;
        }

        /// <summary>
        ///     Creates a new avatar.
        /// </summary>
        /// <param name="file">The new avatar file.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        ///     A created result.
        /// </returns>
        [Route("profile/avatars/")]
        [HttpPost]
        [ProducesResponseType(typeof(AvatarDetails), (int) HttpStatusCode.Created)]
        [SwaggerResponse((int) HttpStatusCode.Created, typeof(AvatarDetails))]
        [SwaggerResponse((int) HttpStatusCode.NotFound)]
        public async Task<IActionResult> Post([ContentType] IFormFile file, CancellationToken cancellationToken)
        {
            if (file == null)
            {
                return new ErrorMessageResult(Resources.Controller_NoBodyDataProvided, HttpStatusCode.BadRequest);
            }

            var profileId = User.Identity.GetClaimValue<Guid>(ClaimType.ProfileId);

            using (var avatar = new Avatar
            {
                ContentType = file.ContentType,
                Data = file.OpenReadStream(),
                ProfileId = profileId,
                Id = Guid.NewGuid()
            })
            {
                var details = await _command.CreateAvatar(avatar, cancellationToken).ConfigureAwait(false);

                var routeValues = new
                {
                    profileId = details.ProfileId,
                    avatarId = details.Id
                };

                return new CreatedAtRouteResult("ProfileAvatar", routeValues, details);
            }
        }
    }
}