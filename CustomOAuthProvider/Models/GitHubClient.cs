namespace CustomOAuthProvider.Models
{
    using System;
    using System.Collections.Generic;
    using System.IO;
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
            var uriBuilder = new UriBuilder(ProfileUrl);
            uriBuilder.AppendQueryArgument("access_token", accessToken);

            var webClient = new WebClient();
            webClient.Headers["User-Agent"] = "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; WOW64; Trident/4.0; SLCC2; .NET CLR 2.0.50727; .NET CLR 3.5.30729; .NET CLR 3.0.30729; Media Center PC 6.0; MDDC)";
            string downloadString = webClient.DownloadString(uriBuilder.Uri);

            var json = JsonConvert.DeserializeObject<dynamic>(downloadString);

            var userData = new Dictionary<string, string>();

            userData.Add("login", (string)json["login"]);
            userData.Add("id", (string)json["id"]);
            userData.Add("avatar_url", (string)json["avatar_url"]);
            userData.Add("gravatar_id", (string)json["gravatar_id"]);
            userData.Add("url", (string)json["url"]);
            userData.Add("name", (string)json["name"]);
            userData.Add("company", (string)json["company"]);
            userData.Add("blog", (string)json["blog"]);
            userData.Add("location", (string)json["location"]);
            userData.Add("email", (string)json["email"]);
            userData.Add("hireable", (string)json["hireable"]);
            userData.Add("bio", (string)json["bio"]);
            userData.Add("public_repos", (string)json["public_repos"]);
            userData.Add("public_gists", (string)json["public_gists"]);
            userData.Add("followers", (string)json["followers"]);
            userData.Add("following", (string)json["following"]);
            userData.Add("html_url", (string)json["html_url"]);
            userData.Add("created_at", (string)json["created_at"]);
            userData.Add("type", (string)json["type"]);
            userData.Add("total_private_repos", (string)json["total_private_repos"]);
            userData.Add("owned_private_repos", (string)json["owned_private_repos"]);
            userData.Add("private_gists", (string)json["private_gists"]);
            userData.Add("disk_usage", (string)json["disk_usage"]);
            userData.Add("collaborators", (string)json["collaborators"]);
            userData.Add("plan_name", (string)json["plan"]["name"]);
            userData.Add("plan_space", (string)json["plan"]["space"]);
            userData.Add("plan_collaborators", (string)json["plan"]["collaborators"]);
            userData.Add("plan_private_repos", (string)json["plan"]["private_repos"]);

            return userData;
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
            var postData = new StringBuilder();
            postData.AppendFormat("client_id={0}", this.clientId);
            postData.AppendFormat("&redirect_uri={0}", HttpUtility.UrlEncode(returnUrl.ToString().ToLower()));
            postData.AppendFormat("&client_secret={0}", this.clientSecret);
            postData.AppendFormat("&code={0}", authorizationCode);

            var webRequest = (HttpWebRequest)WebRequest.Create(AccessTokenUrl);
            webRequest.Method = "POST";
            webRequest.ContentType = "application/x-www-form-urlencoded";

            using (var s = webRequest.GetRequestStream())
            using (var sw = new StreamWriter(s))
            {
                sw.Write(postData.ToString());
            }

            using (var webResponse = webRequest.GetResponse())
            using (var reader = new StreamReader(webResponse.GetResponseStream()))
            {
                var response = reader.ReadToEnd();
                var nameValueCollection = HttpUtility.ParseQueryString(response);
                return nameValueCollection["access_token"];
            }
        }
    }
}