using System;
using Nancy;
using Nancy.Authentication.Forms;
using Nancy.ModelBinding;
using NzbDrone.Core.Configuration;

namespace NzbDrone.Api.Authentication
{
    public class LoginModule : NancyModule
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly IConfigFileProvider _configFileProvider;

        public LoginModule(IAuthenticationService authenticationService, IConfigFileProvider configFileProvider)
            : base("login")
        {
            _authenticationService = authenticationService;
            _configFileProvider = configFileProvider;
            Post["/"] = x => Login(this.Bind<LoginResource>());
        }

        private Response Login(LoginResource resource)
        {
            var user = _authenticationService.Validate(resource.Username, resource.Password);

            if (user == null)
            {
                return new Response
                       {
                           ReasonPhrase = "username and/or password was incorrect",
                           StatusCode = HttpStatusCode.Unauthorized
                       };
            }

            return this.LoginAndRedirect(Guid.Parse(_configFileProvider.ApiKey), fallbackRedirectUrl: "/");
        }
    }
}
