namespace CustomOAuthProvider.App_Start
{
    using Microsoft.Web.WebPages.OAuth;

    public static class AuthConfig
    {
        public static void RegisterAuth()
        {
            OAuthWebSecurity.RegisterFacebookClient(appId: "", appSecret: "");
            OAuthWebSecurity.RegisterGoogleClient();
        }
    }
}