﻿using System;
using System.Collections.Generic;
using System.Linq;
using Abp.Authorization;
using Abp.Collections;
using Abp.Dependency;
using Abp.Runtime.Session;

namespace Abp.Application.Authorization
{
    public class AuthorizeAttributeHelper : ITransientDependency //TODO: Make internal
    {
        public IAbpSession AbpSession { get; set; }

        private readonly IPermissionManager _permissionManager;

        public AuthorizeAttributeHelper(IPermissionManager permissionManager)
        {
            AbpSession = NullAbpSession.Instance;
            _permissionManager = permissionManager;
        }

        public void Authorize(IAbpAuthorizeAttribute authorizeAttribute)
        {
            Authorize(new[] { authorizeAttribute });
        }

        public void Authorize(IEnumerable<IAbpAuthorizeAttribute> authorizeAttributes)
        {
            if (!AbpSession.UserId.HasValue)
            {
                throw new AbpAuthorizationException("No user logged in!");
            }

            foreach (var authorizeAttribute in authorizeAttributes)
            {
                if (authorizeAttribute.Permissions.IsNullOrEmpty())
                {
                    continue;
                }

                if (authorizeAttribute.RequireAllPermissions)
                {
                    if (!authorizeAttribute.Permissions.All(permissionName => _permissionManager.IsGranted(AbpSession.UserId.Value, permissionName)))
                    {
                        throw new AbpAuthorizationException(
                            "Required permissions are not granted. All of these permissions must be granted: " +
                            String.Join(", ", authorizeAttribute.Permissions)
                            );
                    }
                }
                else
                {
                    if (!authorizeAttribute.Permissions.Any(permissionName => _permissionManager.IsGranted(AbpSession.UserId.Value, permissionName)))
                    {
                        throw new AbpAuthorizationException(
                            "Required permissions are not granted. At least one of these permissions must be granted: " +
                            String.Join(", ", authorizeAttribute.Permissions)
                            );
                    }
                }
            }
        }
    }
}