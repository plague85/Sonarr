using System;
using System.Linq;
using Nancy;
using Nancy.Authentication.Basic;
using Nancy.Authentication.Forms;
using Nancy.Security;
using NzbDrone.Api.Extensions;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;

namespace NzbDrone.Api.Authentication
{
    public interface IAuthenticationService : IUserValidator, IUserMapper
    {
        bool IsAuthenticated(NancyContext context);
        IUserIdentity Validate(string username, string password);
    }

    public class AuthenticationService : IAuthenticationService
    {
        private readonly IConfigFileProvider _configFileProvider;
        private static readonly NzbDroneUser AnonymousUser = new NzbDroneUser { UserName = "Anonymous" };
        private static String API_KEY;

        public AuthenticationService(IConfigFileProvider configFileProvider)
        {
            _configFileProvider = configFileProvider;
            API_KEY = configFileProvider.ApiKey;
        }

        public IUserIdentity Validate(string username, string password)
        {
            if (!Enabled)
            {
                return AnonymousUser;
            }

            if (_configFileProvider.Username.Equals(username) &&
                _configFileProvider.Password.Equals(password))
            {
                return new NzbDroneUser { UserName = username };
            }

            return null;
        }

        public IUserIdentity GetUserFromIdentifier(Guid identifier, NancyContext context)
        {
            if (!Enabled)
            {
                return AnonymousUser;
            }

            if (context.CurrentUser != null)
            {
                return new NzbDroneUser { UserName = _configFileProvider.Username };
            }

            return null;
        }

        public bool IsAuthenticated(NancyContext context)
        {
            var apiKey = GetApiKey(context);

            if (context.Request.IsApiRequest())
            {
                return ValidApiKey(apiKey);
            }

            if (context.Request.IsFeedRequest())
            {
                if (!Enabled)
                {
                    return true;
                }

                if (ValidUser(context) || ValidApiKey(apiKey))
                {
                    return true;
                }

                return false;
            }

            if (context.Request.IsLoginRequest())
            {
                return true;
            }

            if (context.Request.IsContentRequest())
            {
                return true;
            }

            if (!Enabled)
            {
                return true;
            }

            if (ValidUser(context))
            {
                return true;
            }

            return false;
        }

        private bool Enabled
        {
            get
            {
                return _configFileProvider.AuthenticationEnabled;
            }
        }

        private bool ValidUser(NancyContext context)
        {
            if (context.CurrentUser != null) return true;

            return false;
        }

        private bool ValidApiKey(string apiKey)
        {
            if (API_KEY.Equals(apiKey)) return true;

            return false;
        }

        private string GetApiKey(NancyContext context)
        {
            var apiKeyHeader = context.Request.Headers["X-Api-Key"].FirstOrDefault();
            var apiKeyQueryString = context.Request.Query["ApiKey"];

            if (!apiKeyHeader.IsNullOrWhiteSpace())
            {
                return apiKeyHeader;
            }

            if (apiKeyQueryString.HasValue)
            {
                return apiKeyQueryString.Value;
            }

            return context.Request.Headers.Authorization;
        }
    }
}
