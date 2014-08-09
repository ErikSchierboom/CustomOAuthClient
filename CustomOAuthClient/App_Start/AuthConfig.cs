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
                consumerKey: "TODO: enter consumer key",
                consumerSecret: "TODO: enter consumer secret", 
                appName: "Custom OAuth Provider");
            OAuthWebSecurity.RegisterClient(trelloClient, "Trello", null);

            // Create and register our custom GitHub client as an OAuth client provider
            var gitHubClient = new GitHubClient(
                clientId: "TODO: enter client ID",
                clientSecret: "TODO: enter client secret");
            OAuthWebSecurity.RegisterClient(gitHubClient, "GitHub", null);
        }
    }
}