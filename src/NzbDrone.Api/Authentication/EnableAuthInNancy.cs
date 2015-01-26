﻿using Nancy;
using Nancy.Authentication.Basic;
using Nancy.Authentication.Forms;
using Nancy.Bootstrapper;
using Nancy.Cryptography;
using NzbDrone.Api.Extensions.Pipelines;

namespace NzbDrone.Api.Authentication
{
    public class EnableAuthInNancy : IRegisterNancyPipeline
    {
        private readonly IAuthenticationService _authenticationService;

        public EnableAuthInNancy(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        public void Register(IPipelines pipelines)
        {
            RegisterFormsAuth(pipelines);
            pipelines.EnableBasicAuthentication(new BasicAuthenticationConfiguration(_authenticationService, "Sonarr"));
            pipelines.BeforeRequest.AddItemToEndOfPipeline(RequiresAuthentication);
        }

        private Response RequiresAuthentication(NancyContext context)
        {
            Response response = null;

            if (!_authenticationService.IsAuthenticated(context))
            {
                response = new Response { StatusCode = HttpStatusCode.Unauthorized };
            }

            return response;
        }

        private void RegisterFormsAuth(IPipelines pipelines)
        {
            //TODO: generate and store a proper passphrase for Hmac/Encryption
            var cryptographyConfiguration = new CryptographyConfiguration(
                new RijndaelEncryptionProvider(new PassphraseKeyGenerator("SuperSecretPass", new byte[] {1, 2, 3, 4, 5, 6, 7, 8})),
                new DefaultHmacProvider(new PassphraseKeyGenerator("UberSuperSecure", new byte[] {1, 2, 3, 4, 5, 6, 7, 8})));

            FormsAuthentication.Enable(pipelines, new FormsAuthenticationConfiguration
            {
                RedirectUrl = "~/login",
                UserMapper = _authenticationService,
                CryptographyConfiguration = cryptographyConfiguration
            });
        }
    }
}
