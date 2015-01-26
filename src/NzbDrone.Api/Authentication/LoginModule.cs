using System;
using Nancy;
using Nancy.Authentication.Forms;
using Nancy.Extensions;
using Nancy.ModelBinding;
using NzbDrone.Core.Configuration;

namespace NzbDrone.Api.Authentication
{
    public class LoginModule : NancyModule
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly IConfigFileProvider _configFileProvider;

        public LoginModule(IAuthenticationService authenticationService, IConfigFileProvider configFileProvider)
        {
            _authenticationService = authenticationService;
            _configFileProvider = configFileProvider;
            Post["/login"] = x => Login(this.Bind<LoginResource>());
        }

        private Response Login(LoginResource resource)
        {
            var user = _authenticationService.Validate(resource.Username, resource.Password);

            if (user == null)
            {
                return Context.GetRedirect("~/login?returnUrl=" + (string)Request.Query.returnUrl);
            }

            DateTime? expiry = null;

            if (resource.RememberMe)
            {
                expiry = DateTime.UtcNow.AddDays(7);
            }

            return this.LoginAndRedirect(Guid.Parse(_configFileProvider.ApiKey), expiry);
        }
    }
}
