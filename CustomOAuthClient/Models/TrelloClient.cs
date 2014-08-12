namespace CustomOAuthClient.Models
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
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// An OAuth client for the Trello.com website. Note: as Trello only supports OAuth v1, this class 
    /// is derived from <see cref="OAuthClient"/> and not from <see cref="OAuth2Client"/>.
    /// </summary>
    public class TrelloClient : OAuthClient
    {
        private const string RequestTokenUrl = "https://trello.com/1/OAuthGetRequestToken";
        private const string AccessTokenUrl = "https://trello.com/1/OAuthGetAccessToken";
        private const string AuthorizeTokenUrl = "https://trello.com/1/OAuthAuthorizeToken";
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
                using (var profileResponseStream = profileResponse.GetResponseStream())
                using (var profileStreamReader = new StreamReader(profileResponseStream))
                {
                    var profileStreamContents = profileStreamReader.ReadToEnd();

                    // Deserialize the profile contents which is returned in JSON format
                    var profile = JsonConvert.DeserializeObject<dynamic>(profileStreamContents);
                    
                    // Return the (successful) authentication result, which also retrieves the user id and username
                    // from the return profile contents
                    return new AuthenticationResult(
                            isSuccessful: true,
                            provider: this.ProviderName,
                            providerUserId: (string)profile.id,
                            userName: (string)profile.username,
                            extraData: GetExtraData(profile));
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
        /// Gets the extra data from the profile.
        /// </summary>
        /// <param name="profile">The profile.</param>
        /// <returns>The extra data.</returns>
        private static Dictionary<string, string> GetExtraData(dynamic profile)
        {
            return new Dictionary<string, string>
                       {
                           { "id", profile.id.ToString() },
                           { "avatarHash", profile.avatarHash.ToString() },
                           { "bio", profile.bio.ToString() },
                           { "confirmed", profile.confirmed.ToString() },
                           { "fullName", profile.fullName.ToString() },
                           { "initials", profile.initials.ToString() },
                           { "memberType", profile.memberType.ToString() },
                           { "status", profile.status.ToString() },
                           { "url", profile.url.ToString() },
                           { "username", profile.username.ToString() },
                           { "avatarSource", profile.avatarSource.ToString() },
                           { "email", profile.email.ToString() },
                           { "gravatarHash", profile.gravatarHash.ToString() },
                           { "newEmail", profile.newEmail.ToString() },
                           { "uploadedAvatarHash", profile.uploadedAvatarHash.ToString() },
                           { "idBoards", ToCommaSeparatedString(profile.idBoards) },
                           { "idBoardsInvited", ToCommaSeparatedString(profile.idBoardsInvited) },
                           { "idBoardsPinned", ToCommaSeparatedString(profile.idBoardsPinned) },
                           { "idOrganizations", ToCommaSeparatedString(profile.idOrganizations) },
                           { "idOrganizationsInvited", ToCommaSeparatedString(profile.idOrganizationsInvited) },
                           { "trophies", ToCommaSeparatedString(profile.trophies) },
                           { "oneTimeMessagesDismissed", ToCommaSeparatedString(profile.oneTimeMessagesDismissed) },
                       };
        }

        /// <summary>
        /// Convert an array property to a comma-separated string.
        /// </summary>
        /// <param name="profileArrayProperty">The profile's array property.</param>
        /// <returns>The comma-separated string representation of the array property.</returns>
        private static string ToCommaSeparatedString(dynamic profileArrayProperty)
        {
            return string.Join(", ", (JArray)profileArrayProperty);
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
                           RequestTokenEndpoint = new MessageReceivingEndpoint(RequestTokenUrl, DeliveryMethods),
                           AccessTokenEndpoint = new MessageReceivingEndpoint(AccessTokenUrl, DeliveryMethods),

                           // As Trello does not know the concept of consuming apps, we need to explicitly add
                           // the name of the app in the query string so the user will be shown what app is 
                           // requesting authorization
                           UserAuthorizationEndpoint = new MessageReceivingEndpoint(AuthorizeTokenUrl + "?name=" + HttpUtility.UrlEncode(appName), DeliveryMethods), 
                           TamperProtectionElements = new ITamperProtectionChannelBindingElement[] { new HmacSha1SigningBindingElement() }
                       };
        }
    }
}