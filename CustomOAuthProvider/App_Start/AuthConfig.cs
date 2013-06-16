namespace CustomOAuthProvider.App_Start
{
    using CustomOAuthProvider.Models;

    using Microsoft.Web.WebPages.OAuth;

    public static class AuthConfig
    {
        public static void RegisterAuth()
        {
            // We register our custom Trello OAuth client
            OAuthWebSecurity.RegisterClient(new TrelloClient(consumerKey: "0bc296ceaf16c662cba9f82e6e7792d0", consumerSecret: "0b3787cacf445efa1c20b4ed32054d5ac46b3cb45091d32df2e905cf146ee1f7", appName: "Custom OAuth Provider"), "Trello", null);
        }
    }
}