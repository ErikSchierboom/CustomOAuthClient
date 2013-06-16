namespace CustomOAuthProvider.App_Start
{
    using CustomOAuthProvider.Models;

    using Microsoft.Web.WebPages.OAuth;

    public static class AuthConfig
    {
        public static void RegisterAuth()
        {
            // We create and register our custom Trello OAuth client
            var trelloClient = new TrelloClient(
                consumerKey: "xxxxxxx", 
                consumerSecret: "yyyyyy", 
                appName: "Custom OAuth Provider");
            OAuthWebSecurity.RegisterClient(trelloClient, "Trello", null);
        }
    }
}