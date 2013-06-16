namespace CustomOAuthProvider.Models
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
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

        protected override AuthenticationResult VerifyAuthenticationCore(AuthorizedTokenResponse response)
        {
            // Create a endpoint with which we will be retrieving the Trello profile of the authenticated user
            var profileEndpoint = new MessageReceivingEndpoint(ProfileUrl, HttpDeliveryMethods.GetRequest);

            // We need to prepare the profile endpoint for authorization using the access token we have
            // just retrieved. If we would not do this, no access token would be sent along with the
            // profile request and the 
            var request = this.WebWorker.PrepareAuthorizedRequest(profileEndpoint, response.AccessToken);

            try
            {
                using (var profileResponse = request.GetResponse())
                using (var responseStream = profileResponse.GetResponseStream())
                using (var readStream = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")))
                {
                    var contents = readStream.ReadToEnd();
                    var profile = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(contents);

                    throw new NotImplementedException();
                    /*
                    return new AuthenticationResult(
                            isSuccessful: true,
                            provider: ProviderName,
                            providerUserId: userId,
                            userName: userName,
                            extraData: extraData);*/

                }

                        /*XDocument document = LoadXDocumentFromStream(responseStream);
                        string userId = document.Root.Element("id").Value;

                        string firstName = document.Root.Element("first-name").Value;
                        string lastName = document.Root.Element("last-name").Value;
                        string userName = firstName + " " + lastName;

                        var extraData = new Dictionary<,> {
                                                {"accesstoken", accessToken}, 
                                                {"name", userName}
                                            };

                        return new AuthenticationResult(
                            isSuccessful: true,
                            provider: ProviderName,
                            providerUserId: userId,
                            userName: userName,
                            extraData: extraData);
                         * */
            }
            catch (Exception exception)
            {
                return new AuthenticationResult(exception);
            }
        }

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