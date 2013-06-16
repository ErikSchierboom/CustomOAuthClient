namespace CustomOAuthProvider.Models
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Web;

    using DotNetOpenAuth.AspNet;
    using DotNetOpenAuth.AspNet.Clients;
    using DotNetOpenAuth.Messaging;
    using DotNetOpenAuth.OAuth;
    using DotNetOpenAuth.OAuth.ChannelElements;
    using DotNetOpenAuth.OAuth.Messages;

    using Newtonsoft.Json;

    /// <summary>
    /// An OAuth client for the Trello.com website. Note: as Trello only supports OAuth v1, this class 
    /// is derived from <see cref="OAuthClient"/> and not from <see cref="OAuth2Client"/>.
    /// </summary>
    public class TrelloClient : OAuthClient
    {
        private const string RequestUrl = "https://trello.com/1/OAuthGetRequestToken";
        private const string AccessUrl = "https://trello.com/1/OAuthGetAccessToken";
        private const string AuthorizeUrl = "https://trello.com/1/OAuthAuthorizeToken";
        private const string ProfileUrl = "https://api.trello.com/1/members/me";

        private const HttpDeliveryMethods DeliveryMethods = HttpDeliveryMethods.GetRequest | HttpDeliveryMethods.AuthorizationHeaderRequest;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrelloClient"/> class.
        /// </summary>
        /// <param name="consumerKey">The consumer key.</param>
        /// <param name="consumerSecret">The consumer secret.</param>
        /// <param name="appName">Name of the app.</param>
        public TrelloClient(string consumerKey, string consumerSecret, string appName)
            : base("Trello", CreateServiceProviderDescription(appName), consumerKey, consumerSecret)
        {
        }

        /// <summary>
        /// Check if authentication succeeded after user is redirected back from the service provider.
        /// </summary>
        /// <param name="response">The response token returned from service provider</param>
        /// <returns>
        /// Authentication result
        /// </returns>
        protected override AuthenticationResult VerifyAuthenticationCore(AuthorizedTokenResponse response)
        {
            // Create a endpoint with which we will be retrieving the Trello profile of the authenticated user
            var profileEndpoint = new MessageReceivingEndpoint(ProfileUrl, HttpDeliveryMethods.GetRequest);

            // We need to prepare the profile endpoint for authorization using the access token we have
            // just retrieved. If we would not do this, no access token would be sent along with the
            // profile request and the request would fail due to it not being authorized
            var request = this.WebWorker.PrepareAuthorizedRequest(profileEndpoint, response.AccessToken);

            try
            {
                using (var profileResponse = request.GetResponse())
                using (var responseStream = profileResponse.GetResponseStream())
                using (var readStream = new StreamReader(responseStream))
                {
                    var contents = readStream.ReadToEnd();

                    // Deserialize the profile contents which is returned in JSON format
                    var profile = JsonConvert.DeserializeObject<dynamic>(contents);

                    // Extract some information from the profile and store it as extra data
                    var extraData = new Dictionary<string, string>
                                        {
                                            { "avatarHash", (string)profile.avatarHash },
                                            { "bio", (string)profile.bio },
                                            { "fullName", (string)profile.fullName },
                                            { "url", (string)profile.url },
                                        };

                    // Return the (successful) authentication result, which also retrieves the user id and username
                    // from the return profile contents
                    return new AuthenticationResult(
                            isSuccessful: true,
                            provider: ProviderName,
                            providerUserId: profile.id,
                            userName: profile.username,
                            extraData: extraData);
                }
            }
            catch (Exception exception)
            {
                // When an exception occurs, we also return that as an authentication result to allow
                // the exception to be gracefully handled
                return new AuthenticationResult(exception);
            }
        }

        /// <summary>
        /// Creates the service provider description. DotNetOpenAuth uses a service provider description
        /// to determine where to send authorization and token requests to.
        /// </summary>
        /// <param name="appName">The name of the app.</param>
        /// <returns>The complete service provider description.</returns>
        private static ServiceProviderDescription CreateServiceProviderDescription(string appName)
        {
            return new ServiceProviderDescription
                       {
                           RequestTokenEndpoint = new MessageReceivingEndpoint(RequestUrl, DeliveryMethods),
                           AccessTokenEndpoint = new MessageReceivingEndpoint(AccessUrl, DeliveryMethods),

                           // As Trello does not know the concept of consuming apps, we need to explicitly add
                           // the name of the app in the query string so the user will be shown what app is 
                           // requesting authorization
                           UserAuthorizationEndpoint = new MessageReceivingEndpoint(AuthorizeUrl + "?name=" + HttpUtility.UrlEncode(appName), DeliveryMethods), 
                           TamperProtectionElements = new ITamperProtectionChannelBindingElement[] { new HmacSha1SigningBindingElement() }
                       };
        }
    }
}