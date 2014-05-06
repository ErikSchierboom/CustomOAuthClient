# CustomOAuthClient

## Introduction
ASP.NET uses the [DotNetOpenAuth library](http://dotnetopenauth.net/) to connect to external applications using OAuth. By default, ASP.NET MVC 4+ has several built-in OAuth clients that let you use OAuth to authenticate with Google, Microsoft, Facebook, Twitter and more. It also allows you to add new OAuth clients. OAuth version 1 *and* 2 are supported.

## Implementation
Creating a new OAuth client is simple. First you need to determine if your OAuth provider uses OAuth v1 or v2. In the v1 case, your custom OAuth client class needs to derive from `OAuthClient`. In the v2 case your custom class needs to derive from `OAuth2Client`.

This project contains a custom client for both OAuth v1 (for [Trello.com](http://trello.com)) and OAuth v2 (for [GitHub.com](http://github.com)).

### OAuth v1 client
For our OAuth v1 client we will be connecting to [Trello.com](Trello.com). As said, we need to be creating a class that derives from the `OAuthClient` class. The first step is to create a suitable constructor. The `OAuthClient` class has three overloads. You can choose to implement all three overloads, but we will only define one that takes the consumer key and secret as parameters:

```c#
public TrelloClient(string consumerKey, string consumerSecret, string appName)
: base("Trello", CreateServiceProviderDescription(appName), consumerKey, consumerSecret)
{
}
```

The **"Trello"** string is the name of the provider. The consumer key and secret are passed to the base constructor, to be user later. The `CreateServiceProviderDescription` method is one that we wrote ourselves. This method returns a `ServiceProviderDescription` which specifies the request token-, access token- and user authorization endpoints. For Trello these are respectively https://trello.com/1/OAuthGetRequestToken, https://trello.com/1/OAuthGetAccessToken and https://trello.com/1/OAuthAuthorizeToken. For each endpoint you need to specify how it should be accessed. For Trello, endpoints need to be accessed through GET requests and with authorization information passed as an HTTP header.

Now we are ready for the last step: overriding the `VerifyAuthenticationCore` method. This method returns an `AuthenticationResult` instance that indicates if the authentication was successful. For Trello, we will do this by trying to access the authenticated user's profile through the https://api.trello.com/1/members/me URL. To be able to call this URL, we need to pass along the access token passed in the method's `AuthorizedTokenResponse` parameter:
    
```c#
var profileEndpoint = new MessageReceivingEndpoint("https://api.trello.com/1/members/me", HttpDeliveryMethods.GetRequest);
var request = this.WebWorker.PrepareAuthorizedRequest(profileEndpoint, response.AccessToken);
```

This code will create a `HttpWebRequest` instance that will send the access token along with the request. Now all that is left is to make a request to the profile URL. If this succeeds (there is no exception), the user has been authenticated successfully. If successful, the profile API method call returns information about the user (like its username and email). We can then pass along this information in the returned `AuthenticationResult` so our authentication code in our account controller can also use this information (and possibly store it).

### OAuth v2 client
For our OAuth v2 client we will be connecting to [GitHub.com](GitHub.com). This time we derive our class from `OAuth2Client`. There are quite some differences between OAuth v1 and v2, one them being that OAuth v2 is far less standardized and thus requires more coding. The constructor is easier though:

```c#
public GitHubClient(string clientId, string clientSecret)
: base("GitHub")
{
    this.clientId = clientId;
    this.clientSecret = clientSecret;
}
```

Here we need to store our API credentials ourselves (in the v1 version this was handled by the base class).

The first step is to implement the `GetServiceLoginUrl` method, which returns an `Uri` to which the user will be redirected:

```c#
protected override Uri GetServiceLoginUrl(Uri returnUrl)
{
    var uriBuilder = new UriBuilder("https://github.com/login/oauth/authorize");
    uriBuilder.AppendQueryArgument("client_id", this.clientId);
    uriBuilder.AppendQueryArgument("redirect_uri", returnUrl.ToString());

    return uriBuilder.Uri;
}
```

Here we create a new URI pointing to the service login URL of GitHub and that has two query parameters: the client ID (which was passed to the constructor) and the URL to which the user will be redirected after authenticating.

Next step is to override the `QueryAccessToken` method. This method takes an authorization code and should return an access token. This can be seen as exchanging an authorization for an access token. The implementation is rather straightforward:

```c#
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
``` 

We once again create an URI, this time using a `NameValueCollection`. The client ID and secret are passing as parameters, as well as the return URL and, importantly, the authorization code. After executing the request, we extract the access token from the returned code.

The last step is to return the user's data based on an access token. For this we make a call to https://api.github.com/user, passing along the access token in the URL. If the request is successful, the user was authenticated successfully and we can extract the user data (like username and email) from the response.

### Building
There is one thing to do before the application is ready to be used. You have to fill in your Trello and/or GitHub's API credentials. You do this in [AuthConfig.cs](CustomOAuthProvider/App_Start/AuthConfig.cs). There you'll find an instance of the `TrelloClient` and the `GitHubClient` being created. The missing API credentials are marked as **TODO**. 

You can find your Trello credentials by going to [https://trello.com/1/appKey/generate](https://trello.com/1/appKey/generate). Your GitHub credentials can be created by going to the [applications section of your account settings](https://github.com/settings/applications). There you can click on the "Register new application" button and create a new application with its own API credentials.

After you have filled in the credentials in [AuthConfig.cs](CustomOAuthProvider/App_Start/AuthConfig.cs) you are ready to run the application.

## Usage
After running the application, just click on the **Trello** or **GitHub** button and you will be redirected to that provider to let you authenticate using your Trello or GitHub account. After authentication has completed (either successfully or not), you will be returned to the website.

## License
[Apache License 2.0](LICENSE.md)
