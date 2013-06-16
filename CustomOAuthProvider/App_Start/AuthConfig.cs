namespace CustomOAuthProvider.App_Start
{
    using CustomOAuthProvider.Models;

    using Microsoft.Web.WebPages.OAuth;

    public static class AuthConfig
    {
        public static void RegisterAuth()
        {
            // Create and register our custom Trello client as an OAuth client provider
            var trelloClient = new TrelloClient(
                consumerKey: "aaaaa",
                consumerSecret: "bbbbbb", 
                appName: "Custom OAuth Provider");
            OAuthWebSecurity.RegisterClient(trelloClient, "Trello", null);

            // Create and register our custom GitHub client as an OAuth client provider
            var gitHubClient = new GitHubClient(
                clientId: "ddddd",
                clientSecret: "eeeee");
            OAuthWebSecurity.RegisterClient(gitHubClient, "GitHub", null);
        }
    }
}