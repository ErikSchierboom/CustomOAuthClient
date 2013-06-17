namespace CustomOAuthProvider.Models
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Net;
    using System.Text;
    using System.Web;

    using DotNetOpenAuth.AspNet.Clients;
    using DotNetOpenAuth.Messaging;

    using Newtonsoft.Json;

    /// <summary>
    /// An OAuth client for the GitHub.com website. Note: as GitHub only supports OAuth v2, this class 
    /// is derived from <see cref="OAuth2Client"/> and not from <see cref="OAuthClient"/>.
    /// </summary>
    public class GitHubClient : OAuth2Client
    {
        private const string AccessTokenUrl = "https://github.com/login/oauth/access_token";
        private const string AuthorizeUrl = "https://github.com/login/oauth/authorize";
        private const string ProfileUrl = "https://api.github.com/user";

        private const string UserAgent = "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; WOW64; Trident/4.0; SLCC2; .NET CLR 2.0.50727; .NET CLR 3.5.30729; .NET CLR 3.0.30729; Media Center PC 6.0; MDDC)";

        private readonly string clientId;
        private readonly string clientSecret;

        /// <summary>
        /// Initializes a new instance of the <see cref="GitHubClient"/> class.
        /// </summary>
        /// <param name="clientId">The client id.</param>
        /// <param name="clientSecret">The client secret.</param>
        public GitHubClient(string clientId, string clientSecret)
            : base("GitHub")
        {
            this.clientId = clientId;
            this.clientSecret = clientSecret;
        }

        /// <summary>
        /// Gets the full url pointing to the login page for this client. The url should include the specified return url so that when the login completes, user is redirected back to that url.
        /// </summary>
        /// <param name="returnUrl">The return URL.</param>
        /// <returns>
        /// An absolute URL.
        /// </returns>
        protected override Uri GetServiceLoginUrl(Uri returnUrl)
        {
            var uriBuilder = new UriBuilder(AuthorizeUrl);
            uriBuilder.AppendQueryArgument("client_id", this.clientId);
            uriBuilder.AppendQueryArgument("redirect_uri", returnUrl.ToString());

            return uriBuilder.Uri;
        }

        /// <summary>
        /// Given the access token, gets the logged-in user's data. The returned dictionary must include two keys 'id', and 'username'.
        /// </summary>
        /// <param name="accessToken">The access token of the current user.</param>
        /// <returns>
        /// A dictionary contains key-value pairs of user data
        /// </returns>
        protected override IDictionary<string, string> GetUserData(string accessToken)
        {
            using (var webClient = CreateWebClient())
            {
                var uriBuilder = new UriBuilder(ProfileUrl);
                uriBuilder.AppendQueryArgument("access_token", accessToken);

                var profileResponse = webClient.DownloadString(uriBuilder.Uri);
                var profile = JsonConvert.DeserializeObject<dynamic>(profileResponse);

                return new Dictionary<string, string>
                       {
                           { "login", profile.login.ToString() }, 
                           { "id", profile.id.ToString() }, 
                           { "avatar_url", profile.avatar_url.ToString() }, 
                           { "gravatar_id", profile.gravatar_id.ToString() }, 
                           { "url", profile.url.ToString() }, 
                           { "name", profile.name.ToString() }, 
                           { "company", profile.company.ToString() }, 
                           { "blog", profile.blog.ToString() }, 
                           { "location", profile.location.ToString() }, 
                           { "email", profile.email.ToString() },
                           { "hireable", profile.hireable.ToString() }, 
                           { "bio", profile.bio.ToString() }, 
                           { "public_repos", profile.public_repos.ToString() }, 
                           { "public_gists", profile.public_gists.ToString() }, 
                           { "followers", profile.followers.ToString() }, 
                           { "following", profile.following.ToString() },
                           { "html_url", profile.html_url.ToString() }, 
                           { "created_at", profile.created_at.ToString() }, 
                           { "type", profile.type.ToString() }, 
                           { "total_private_repos", profile.total_private_repos.ToString() }, 
                           { "owned_private_repos", profile.owned_private_repos.ToString() }, 
                           { "private_gists", profile.private_gists.ToString() },
                           { "disk_usage", profile.disk_usage.ToString() },
                           { "collaborators", profile.collaborators.ToString() }, 
                           { "plan_name", profile.plan.name.ToString() }, 
                           { "plan_space", profile.plan.space.ToString() }, 
                           { "plan_collaborators", profile.plan.collaborators.ToString() }, 
                           { "plan_private_repos", profile.plan.private_repos.ToString() }
                       };
            }
        }

        /// <summary>
        /// Queries the access token from the specified authorization code.
        /// </summary>
        /// <param name="returnUrl">The return URL.</param>
        /// <param name="authorizationCode">The authorization code.</param>
        /// <returns>
        /// The access token
        /// </returns>
        protected override string QueryAccessToken(Uri returnUrl, string authorizationCode)
        {
            using (var webClient = CreateWebClient())
            {
                var parameters = new NameValueCollection
                                          {
                                              { "client_id", this.clientId },
                                              { "client_secret", this.clientSecret },
                                              { "redirect_uri", returnUrl.ToString() },
                                              { "code", authorizationCode },
                                          };

                var accessTokenResponse = Encoding.UTF8.GetString(webClient.UploadValues(AccessTokenUrl, parameters));
                var parsedAccessTokenResponse = HttpUtility.ParseQueryString(accessTokenResponse);
                return parsedAccessTokenResponse["access_token"];
            }
        }

        /// <summary>
        /// Creates a web client instance.
        /// </summary>
        /// <returns>The web client.</returns>
        private static WebClient CreateWebClient()
        {
            var webClient = new WebClient();
            webClient.Headers["User-Agent"] = UserAgent;
            return webClient;
        }
    }
}