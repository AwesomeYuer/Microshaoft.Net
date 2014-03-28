//namespace Microshaoft.SharePointApps
//{
//    using Microsoft.IdentityModel;
//    using Microsoft.IdentityModel.S2S.Protocols.OAuth2;
//    using Microsoft.IdentityModel.S2S.Tokens;
//    using Microsoft.SharePoint.Client;
//    using Microsoft.SharePoint.Client.EventReceivers;
//    using System;
//    using System.Collections.Generic;
//    using System.Collections.ObjectModel;
//    using System.Globalization;
//    using System.IdentityModel.Selectors;
//    using System.IdentityModel.Tokens;
//    using System.IO;
//    using System.Linq;
//    using System.Net;
//    using System.Security.Cryptography.X509Certificates;
//    using System.Security.Principal;
//    using System.ServiceModel;
//    using System.Text;
//    using System.Web;
//    using System.Web.Configuration;
//    using System.Web.Script.Serialization;
//    using AudienceRestriction = Microsoft.IdentityModel.Tokens.AudienceRestriction;
//    using AudienceUriValidationFailedException = Microsoft.IdentityModel.Tokens.AudienceUriValidationFailedException;
//    using SecurityTokenHandlerConfiguration = Microsoft.IdentityModel.Tokens.SecurityTokenHandlerConfiguration;
//    using X509SigningCredentials = Microsoft.IdentityModel.SecurityTokenService.X509SigningCredentials;
//    public static class TokenHelper
//    {
//        #region public fields
//        /// <summary>
//        /// SharePoint principal.
//        /// </summary>
//        public const string SharePointPrincipal = "00000003-0000-0ff1-ce00-000000000000";
//        /// <summary>
//        /// Lifetime of HighTrust access token, 12 hours.
//        /// </summary>
//        public static readonly TimeSpan HighTrustAccessTokenLifetime = TimeSpan.FromHours(12.0);
//        #endregion public fields
//        #region public methods
//        /// <summary>
//        /// Retrieves the context token string from the specified request by looking for well-known parameter names in the 
//        /// POSTed form parameters and the querystring. Returns null if no context token is found.
//        /// </summary>
//        /// <param name="request">HttpRequest in which to look for a context token</param>
//        /// <returns>The context token string</returns>
//        public static string GetContextTokenFromRequest(HttpRequest request)
//        {
//            return GetContextTokenFromRequest(new HttpRequestWrapper(request));
//        }
//        /// <summary>
//        /// Retrieves the context token string from the specified request by looking for well-known parameter names in the 
//        /// POSTed form parameters and the querystring. Returns null if no context token is found.
//        /// </summary>
//        /// <param name="request">HttpRequest in which to look for a context token</param>
//        /// <returns>The context token string</returns>
//        public static string GetContextTokenFromRequest(HttpRequestBase request)
//        {
//            string[] paramNames = { "AppContext", "AppContextToken", "AccessToken", "SPAppToken" };
//            foreach (string paramName in paramNames)
//            {
//                if (!string.IsNullOrEmpty(request.Form[paramName]))
//                {
//                    return request.Form[paramName];
//                }
//                if (!string.IsNullOrEmpty(request.QueryString[paramName]))
//                {
//                    return request.QueryString[paramName];
//                }
//            }
//            return null;
//        }
//        /// <summary>
//        /// Validate that a specified context token string is intended for this application based on the parameters 
//        /// specified in web.config. Parameters used from web.config used for validation include ClientId, 
//        /// HostedAppHostNameOverride, HostedAppHostName, ClientSecret, and Realm (if it is specified). If HostedAppHostNameOverride is present,
//        /// it will be used for validation. Otherwise, if the <paramref name="appHostName"/> is not 
//        /// null, it is used for validation instead of the web.config's HostedAppHostName. If the token is invalid, an 
//        /// exception is thrown. If the token is valid, TokenHelper's static STS metadata url is updated based on the token contents
//        /// and a JsonWebSecurityToken based on the context token is returned.
//        /// </summary>
//        /// <param name="contextTokenString">The context token to validate</param>
//        /// <param name="appHostName">The URL authority, consisting of  Domain Name System (DNS) host name or IP address and the port number, to use for token audience validation.
//        /// If null, HostedAppHostName web.config setting is used instead. HostedAppHostNameOverride web.config setting, if present, will be used 
//        /// for validation instead of <paramref name="appHostName"/> .</param>
//        /// <returns>A JsonWebSecurityToken based on the context token.</returns>
//        public static SharePointContextToken ReadAndValidateContextToken(string contextTokenString, string appHostName = null)
//        {
//            JsonWebSecurityTokenHandler tokenHandler = CreateJsonWebSecurityTokenHandler();
//            SecurityToken securityToken = tokenHandler.ReadToken(contextTokenString);
//            JsonWebSecurityToken jsonToken = securityToken as JsonWebSecurityToken;
//            SharePointContextToken token = SharePointContextToken.Create(jsonToken);
//            string stsAuthority = (new Uri(token.SecurityTokenServiceUri)).Authority;
//            int firstDot = stsAuthority.IndexOf('.');
//            GlobalEndPointPrefix = stsAuthority.Substring(0, firstDot);
//            AcsHostUrl = stsAuthority.Substring(firstDot + 1);
//            tokenHandler.ValidateToken(jsonToken);
//            string[] acceptableAudiences;
//            if (!String.IsNullOrEmpty(HostedAppHostNameOverride))
//            {
//                acceptableAudiences = HostedAppHostNameOverride.Split(';');
//            }
//            else if (appHostName == null)
//            {
//                acceptableAudiences = new[] { HostedAppHostName };
//            }
//            else
//            {
//                acceptableAudiences = new[] { appHostName };
//            }
//            bool validationSuccessful = false;
//            string realm = Realm ?? token.Realm;
//            foreach (var audience in acceptableAudiences)
//            {
//                string principal = GetFormattedPrincipal(ClientId, audience, realm);
//                if (StringComparer.OrdinalIgnoreCase.Equals(token.Audience, principal))
//                {
//                    validationSuccessful = true;
//                    break;
//                }
//            }
//            if (!validationSuccessful)
//            {
//                throw new AudienceUriValidationFailedException(
//                    String.Format(CultureInfo.CurrentCulture,
//                    "\"{0}\" is not the intended audience \"{1}\"", String.Join(";", acceptableAudiences), token.Audience));
//            }
//            return token;
//        }
//        /// <summary>
//        /// Retrieves an access token from ACS to call the source of the specified context token at the specified 
//        /// targetHost. The targetHost must be registered for the principal that sent the context token.
//        /// </summary>
//        /// <param name="contextToken">Context token issued by the intended access token audience</param>
//        /// <param name="targetHost">Url authority of the target principal</param>
//        /// <returns>An access token with an audience matching the context token's source</returns>
//        public static OAuth2AccessTokenResponse GetAccessToken(SharePointContextToken contextToken, string targetHost)
//        {
//            string targetPrincipalName = contextToken.TargetPrincipalName;
//            // Extract the refreshToken from the context token
//            string refreshToken = contextToken.RefreshToken;
//            if (String.IsNullOrEmpty(refreshToken))
//            {
//                return null;
//            }
//            string targetRealm = Realm ?? contextToken.Realm;
//            return GetAccessToken(refreshToken,
//                                  targetPrincipalName,
//                                  targetHost,
//                                  targetRealm);
//        }
//        /// <summary>
//        /// Uses the specified authorization code to retrieve an access token from ACS to call the specified principal 
//        /// at the specified targetHost. The targetHost must be registered for target principal.  If specified realm is 
//        /// null, the "Realm" setting in web.config will be used instead.
//        /// </summary>
//        /// <param name="authorizationCode">Authorization code to exchange for access token</param>
//        /// <param name="targetPrincipalName">Name of the target principal to retrieve an access token for</param>
//        /// <param name="targetHost">Url authority of the target principal</param>
//        /// <param name="targetRealm">Realm to use for the access token's nameid and audience</param>
//        /// <param name="redirectUri">Redirect URI registerd for this app</param>
//        /// <returns>An access token with an audience of the target principal</returns>
//        public static OAuth2AccessTokenResponse GetAccessToken(
//            string authorizationCode,
//            string targetPrincipalName,
//            string targetHost,
//            string targetRealm,
//            Uri redirectUri)
//        {
//            if (targetRealm == null)
//            {
//                targetRealm = Realm;
//            }
//            string resource = GetFormattedPrincipal(targetPrincipalName, targetHost, targetRealm);
//            string clientId = GetFormattedPrincipal(ClientId, null, targetRealm);
//            // Create request for token. The RedirectUri is null here.  This will fail if redirect uri is registered
//            OAuth2AccessTokenRequest oauth2Request =
//                OAuth2MessageFactory.CreateAccessTokenRequestWithAuthorizationCode(
//                    clientId,
//                    ClientSecret,
//                    authorizationCode,
//                    redirectUri,
//                    resource);
//            // Get token
//            OAuth2S2SClient client = new OAuth2S2SClient();
//            OAuth2AccessTokenResponse oauth2Response;
//            try
//            {
//                oauth2Response =
//                    client.Issue(AcsMetadataParser.GetStsUrl(targetRealm), oauth2Request) as OAuth2AccessTokenResponse;
//            }
//            catch (WebException wex)
//            {
//                using (StreamReader sr = new StreamReader(wex.Response.GetResponseStream()))
//                {
//                    string responseText = sr.ReadToEnd();
//                    throw new WebException(wex.Message + " - " + responseText, wex);
//                }
//            }
//            return oauth2Response;
//        }
//        /// <summary>
//        /// Uses the specified refresh token to retrieve an access token from ACS to call the specified principal 
//        /// at the specified targetHost. The targetHost must be registered for target principal.  If specified realm is 
//        /// null, the "Realm" setting in web.config will be used instead.
//        /// </summary>
//        /// <param name="refreshToken">Refresh token to exchange for access token</param>
//        /// <param name="targetPrincipalName">Name of the target principal to retrieve an access token for</param>
//        /// <param name="targetHost">Url authority of the target principal</param>
//        /// <param name="targetRealm">Realm to use for the access token's nameid and audience</param>
//        /// <returns>An access token with an audience of the target principal</returns>
//        public static OAuth2AccessTokenResponse GetAccessToken(
//            string refreshToken,
//            string targetPrincipalName,
//            string targetHost,
//            string targetRealm)
//        {
//            if (targetRealm == null)
//            {
//                targetRealm = Realm;
//            }
//            string resource = GetFormattedPrincipal(targetPrincipalName, targetHost, targetRealm);
//            string clientId = GetFormattedPrincipal(ClientId, null, targetRealm);
//            OAuth2AccessTokenRequest oauth2Request = OAuth2MessageFactory.CreateAccessTokenRequestWithRefreshToken(clientId, ClientSecret, refreshToken, resource);
//            // Get token
//            OAuth2S2SClient client = new OAuth2S2SClient();
//            OAuth2AccessTokenResponse oauth2Response;
//            try
//            {
//                oauth2Response =
//                    client.Issue(AcsMetadataParser.GetStsUrl(targetRealm), oauth2Request) as OAuth2AccessTokenResponse;
//            }
//            catch (WebException wex)
//            {
//                using (StreamReader sr = new StreamReader(wex.Response.GetResponseStream()))
//                {
//                    string responseText = sr.ReadToEnd();
//                    throw new WebException(wex.Message + " - " + responseText, wex);
//                }
//            }
//            return oauth2Response;
//        }
//        /// <summary>
//        /// Retrieves an app-only access token from ACS to call the specified principal 
//        /// at the specified targetHost. The targetHost must be registered for target principal.  If specified realm is 
//        /// null, the "Realm" setting in web.config will be used instead.
//        /// </summary>
//        /// <param name="targetPrincipalName">Name of the target principal to retrieve an access token for</param>
//        /// <param name="targetHost">Url authority of the target principal</param>
//        /// <param name="targetRealm">Realm to use for the access token's nameid and audience</param>
//        /// <returns>An access token with an audience of the target principal</returns>
//        public static OAuth2AccessTokenResponse GetAppOnlyAccessToken(
//            string targetPrincipalName,
//            string targetHost,
//            string targetRealm)
//        {
//            if (targetRealm == null)
//            {
//                targetRealm = Realm;
//            }
//            string resource = GetFormattedPrincipal(targetPrincipalName, targetHost, targetRealm);
//            string clientId = GetFormattedPrincipal(ClientId, HostedAppHostName, targetRealm);
//            OAuth2AccessTokenRequest oauth2Request = OAuth2MessageFactory.CreateAccessTokenRequestWithClientCredentials(clientId, ClientSecret, resource);
//            oauth2Request.Resource = resource;
//            // Get token
//            OAuth2S2SClient client = new OAuth2S2SClient();
//            OAuth2AccessTokenResponse oauth2Response;
//            try
//            {
//                oauth2Response =
//                    client.Issue(AcsMetadataParser.GetStsUrl(targetRealm), oauth2Request) as OAuth2AccessTokenResponse;
//            }
//            catch (WebException wex)
//            {
//                using (StreamReader sr = new StreamReader(wex.Response.GetResponseStream()))
//                {
//                    string responseText = sr.ReadToEnd();
//                    throw new WebException(wex.Message + " - " + responseText, wex);
//                }
//            }
//            return oauth2Response;
//        }
//        /// <summary>
//        /// Creates a client context based on the properties of a remote event receiver
//        /// </summary>
//        /// <param name="properties">Properties of a remote event receiver</param>
//        /// <returns>A ClientContext ready to call the web where the event originated</returns>
//        public static ClientContext CreateRemoteEventReceiverClientContext(SPRemoteEventProperties properties)
//        {
//            Uri sharepointUrl;
//            if (properties.ListEventProperties != null)
//            {
//                sharepointUrl = new Uri(properties.ListEventProperties.WebUrl);
//            }
//            else if (properties.ItemEventProperties != null)
//            {
//                sharepointUrl = new Uri(properties.ItemEventProperties.WebUrl);
//            }
//            else if (properties.WebEventProperties != null)
//            {
//                sharepointUrl = new Uri(properties.WebEventProperties.FullUrl);
//            }
//            else
//            {
//                return null;
//            }
//            if (IsHighTrustApp())
//            {
//                return GetS2SClientContextWithWindowsIdentity(sharepointUrl, null);
//            }
//            return CreateAcsClientContextForUrl(properties, sharepointUrl);
//        }
//        /// <summary>
//        /// Creates a client context based on the properties of an app event
//        /// </summary>
//        /// <param name="properties">Properties of an app event</param>
//        /// <param name="useAppWeb">True to target the app web, false to target the host web</param>
//        /// <returns>A ClientContext ready to call the app web or the parent web</returns>
//        public static ClientContext CreateAppEventClientContext(SPRemoteEventProperties properties, bool useAppWeb)
//        {
//            if (properties.AppEventProperties == null)
//            {
//                return null;
//            }
//            Uri sharepointUrl = useAppWeb ? properties.AppEventProperties.AppWebFullUrl : properties.AppEventProperties.HostWebFullUrl;
//            if (IsHighTrustApp())
//            {
//                return GetS2SClientContextWithWindowsIdentity(sharepointUrl, null);
//            }
//            return CreateAcsClientContextForUrl(properties, sharepointUrl);
//        }
//        /// <summary>
//        /// Retrieves an access token from ACS using the specified authorization code, and uses that access token to 
//        /// create a client context
//        /// </summary>
//        /// <param name="targetUrl">Url of the target SharePoint site</param>
//        /// <param name="authorizationCode">Authorization code to use when retrieving the access token from ACS</param>
//        /// <param name="redirectUri">Redirect URI registerd for this app</param>
//        /// <returns>A ClientContext ready to call targetUrl with a valid access token</returns>
//        public static ClientContext GetClientContextWithAuthorizationCode(
//            string targetUrl,
//            string authorizationCode,
//            Uri redirectUri)
//        {
//            return GetClientContextWithAuthorizationCode(targetUrl, SharePointPrincipal, authorizationCode, GetRealmFromTargetUrl(new Uri(targetUrl)), redirectUri);
//        }
//        /// <summary>
//        /// Retrieves an access token from ACS using the specified authorization code, and uses that access token to 
//        /// create a client context
//        /// </summary>
//        /// <param name="targetUrl">Url of the target SharePoint site</param>
//        /// <param name="targetPrincipalName">Name of the target SharePoint principal</param>
//        /// <param name="authorizationCode">Authorization code to use when retrieving the access token from ACS</param>
//        /// <param name="targetRealm">Realm to use for the access token's nameid and audience</param>
//        /// <param name="redirectUri">Redirect URI registerd for this app</param>
//        /// <returns>A ClientContext ready to call targetUrl with a valid access token</returns>
//        public static ClientContext GetClientContextWithAuthorizationCode(
//            string targetUrl,
//            string targetPrincipalName,
//            string authorizationCode,
//            string targetRealm,
//            Uri redirectUri)
//        {
//            Uri targetUri = new Uri(targetUrl);
//            string accessToken =
//                GetAccessToken(authorizationCode, targetPrincipalName, targetUri.Authority, targetRealm, redirectUri).AccessToken;
//            return GetClientContextWithAccessToken(targetUrl, accessToken);
//        }
//        /// <summary>
//        /// Uses the specified access token to create a client context
//        /// </summary>
//        /// <param name="targetUrl">Url of the target SharePoint site</param>
//        /// <param name="accessToken">Access token to be used when calling the specified targetUrl</param>
//        /// <returns>A ClientContext ready to call targetUrl with the specified access token</returns>
//        public static ClientContext GetClientContextWithAccessToken(string targetUrl, string accessToken)
//        {
//            ClientContext clientContext = new ClientContext(targetUrl);
//            clientContext.AuthenticationMode = ClientAuthenticationMode.Anonymous;
//            clientContext.FormDigestHandlingEnabled = false;
//            clientContext.ExecutingWebRequest +=
//                delegate(object oSender, WebRequestEventArgs webRequestEventArgs)
//                {
//                    webRequestEventArgs.WebRequestExecutor.RequestHeaders["Authorization"] =
//                        "Bearer " + accessToken;
//                };
//            return clientContext;
//        }
//        /// <summary>
//        /// Retrieves an access token from ACS using the specified context token, and uses that access token to create
//        /// a client context
//        /// </summary>
//        /// <param name="targetUrl">Url of the target SharePoint site</param>
//        /// <param name="contextTokenString">Context token received from the target SharePoint site</param>
//        /// <param name="appHostUrl">Url authority of the hosted app.  If this is null, the value in the HostedAppHostName
//        /// of web.config will be used instead</param>
//        /// <returns>A ClientContext ready to call targetUrl with a valid access token</returns>
//        public static ClientContext GetClientContextWithContextToken(
//            string targetUrl,
//            string contextTokenString,
//            string appHostUrl)
//        {
//            SharePointContextToken contextToken = ReadAndValidateContextToken(contextTokenString, appHostUrl);
//            Uri targetUri = new Uri(targetUrl);
//            string accessToken = GetAccessToken(contextToken, targetUri.Authority).AccessToken;
//            return GetClientContextWithAccessToken(targetUrl, accessToken);
//        }
//        /// <summary>
//        /// Returns the SharePoint url to which the app should redirect the browser to request consent and get back
//        /// an authorization code.
//        /// </summary>
//        /// <param name="contextUrl">Absolute Url of the SharePoint site</param>
//        /// <param name="scope">Space-delimited permissions to request from the SharePoint site in "shorthand" format 
//        /// (e.g. "Web.Read Site.Write")</param>
//        /// <returns>Url of the SharePoint site's OAuth authorization page</returns>
//        public static string GetAuthorizationUrl(string contextUrl, string scope)
//        {
//            return string.Format(
//                "{0}{1}?IsDlg=1&client_id={2}&scope={3}&response_type=code",
//                EnsureTrailingSlash(contextUrl),
//                AuthorizationPage,
//                ClientId,
//                scope);
//        }
//        /// <summary>
//        /// Returns the SharePoint url to which the app should redirect the browser to request consent and get back
//        /// an authorization code.
//        /// </summary>
//        /// <param name="contextUrl">Absolute Url of the SharePoint site</param>
//        /// <param name="scope">Space-delimited permissions to request from the SharePoint site in "shorthand" format
//        /// (e.g. "Web.Read Site.Write")</param>
//        /// <param name="redirectUri">Uri to which SharePoint should redirect the browser to after consent is 
//        /// granted</param>
//        /// <returns>Url of the SharePoint site's OAuth authorization page</returns>
//        public static string GetAuthorizationUrl(string contextUrl, string scope, string redirectUri)
//        {
//            return string.Format(
//                "{0}{1}?IsDlg=1&client_id={2}&scope={3}&response_type=code&redirect_uri={4}",
//                EnsureTrailingSlash(contextUrl),
//                AuthorizationPage,
//                ClientId,
//                scope,
//                redirectUri);
//        }
//        /// <summary>
//        /// Returns the SharePoint url to which the app should redirect the browser to request a new context token.
//        /// </summary>
//        /// <param name="contextUrl">Absolute Url of the SharePoint site</param>
//        /// <param name="redirectUri">Uri to which SharePoint should redirect the browser to with a context token</param>
//        /// <returns>Url of the SharePoint site's context token redirect page</returns>
//        public static string GetAppContextTokenRequestUrl(string contextUrl, string redirectUri)
//        {
//            return string.Format(
//                "{0}{1}?client_id={2}&redirect_uri={3}",
//                EnsureTrailingSlash(contextUrl),
//                RedirectPage,
//                ClientId,
//                redirectUri);
//        }
//        /// <summary>
//        /// Retrieves an S2S access token signed by the application's private certificate on behalf of the specified 
//        /// WindowsIdentity and intended for the SharePoint at the targetApplicationUri. If no Realm is specified in 
//        /// web.config, an auth challenge will be issued to the targetApplicationUri to discover it.
//        /// </summary>
//        /// <param name="targetApplicationUri">Url of the target SharePoint site</param>
//        /// <param name="identity">Windows identity of the user on whose behalf to create the access token</param>
//        /// <returns>An access token with an audience of the target principal</returns>
//        public static string GetS2SAccessTokenWithWindowsIdentity(
//            Uri targetApplicationUri,
//            WindowsIdentity identity)
//        {
//            string realm = string.IsNullOrEmpty(Realm) ? GetRealmFromTargetUrl(targetApplicationUri) : Realm;
//            JsonWebTokenClaim[] claims = identity != null ? GetClaimsWithWindowsIdentity(identity) : null;
//            return GetS2SAccessTokenWithClaims(targetApplicationUri.Authority, realm, claims);
//        }
//        /// <summary>
//        /// Retrieves an S2S client context with an access token signed by the application's private certificate on 
//        /// behalf of the specified WindowsIdentity and intended for application at the targetApplicationUri using the 
//        /// targetRealm. If no Realm is specified in web.config, an auth challenge will be issued to the 
//        /// targetApplicationUri to discover it.
//        /// </summary>
//        /// <param name="targetApplicationUri">Url of the target SharePoint site</param>
//        /// <param name="identity">Windows identity of the user on whose behalf to create the access token</param>
//        /// <returns>A ClientContext using an access token with an audience of the target application</returns>
//        public static ClientContext GetS2SClientContextWithWindowsIdentity(
//            Uri targetApplicationUri,
//            WindowsIdentity identity)
//        {
//            string realm = string.IsNullOrEmpty(Realm) ? GetRealmFromTargetUrl(targetApplicationUri) : Realm;
//            JsonWebTokenClaim[] claims = identity != null ? GetClaimsWithWindowsIdentity(identity) : null;
//            string accessToken = GetS2SAccessTokenWithClaims(targetApplicationUri.Authority, realm, claims);
//            return GetClientContextWithAccessToken(targetApplicationUri.ToString(), accessToken);
//        }
//        /// <summary>
//        /// Get authentication realm from SharePoint
//        /// </summary>
//        /// <param name="targetApplicationUri">Url of the target SharePoint site</param>
//        /// <returns>String representation of the realm GUID</returns>
//        public static string GetRealmFromTargetUrl(Uri targetApplicationUri)
//        {
//            WebRequest request = WebRequest.Create(targetApplicationUri + "/_vti_bin/client.svc");
//            request.Headers.Add("Authorization: Bearer ");
//            try
//            {
//                using (request.GetResponse())
//                {
//                }
//            }
//            catch (WebException e)
//            {
//                if (e.Response == null)
//                {
//                    return null;
//                }
//                string bearerResponseHeader = e.Response.Headers["WWW-Authenticate"];
//                if (string.IsNullOrEmpty(bearerResponseHeader))
//                {
//                    return null;
//                }
//                const string bearer = "Bearer realm=\"";
//                int bearerIndex = bearerResponseHeader.IndexOf(bearer, StringComparison.Ordinal);
//                if (bearerIndex < 0)
//                {
//                    return null;
//                }
//                int realmIndex = bearerIndex + bearer.Length;
//                if (bearerResponseHeader.Length >= realmIndex + 36)
//                {
//                    string targetRealm = bearerResponseHeader.Substring(realmIndex, 36);
//                    Guid realmGuid;
//                    if (Guid.TryParse(targetRealm, out realmGuid))
//                    {
//                        return targetRealm;
//                    }
//                }
//            }
//            return null;
//        }
//        /// <summary>
//        /// Determines if this is a high trust app.
//        /// </summary>
//        /// <returns>True if this is a high trust app.</returns>
//        public static bool IsHighTrustApp()
//        {
//            return SigningCredentials != null;
//        }
//        /// <summary>
//        /// Ensures that the specified URL ends with '/' if it is not null or empty.
//        /// </summary>
//        /// <param name="url">The url.</param>
//        /// <returns>The url ending with '/' if it is not null or empty.</returns>
//        public static string EnsureTrailingSlash(string url)
//        {
//            if (!string.IsNullOrEmpty(url) && url[url.Length - 1] != '/')
//            {
//                return url + "/";
//            }
//            return url;
//        }
//        #endregion
//        #region private fields
//        //
//        // Configuration Constants
//        //        
//        private const string AuthorizationPage = "_layouts/15/OAuthAuthorize.aspx";
//        private const string RedirectPage = "_layouts/15/AppRedirect.aspx";
//        private const string AcsPrincipalName = "00000001-0000-0000-c000-000000000000";
//        private const string AcsMetadataEndPointRelativeUrl = "metadata/json/1";
//        private const string S2SProtocol = "OAuth2";
//        private const string DelegationIssuance = "DelegationIssuance1.0";
//        private const string NameIdentifierClaimType = JsonWebTokenConstants.ReservedClaims.NameIdentifier;
//        private const string TrustedForImpersonationClaimType = "trustedfordelegation";
//        private const string ActorTokenClaimType = JsonWebTokenConstants.ReservedClaims.ActorToken;
//        //
//        // Environment Constants
//        //
//        private static string GlobalEndPointPrefix = "accounts";
//        private static string AcsHostUrl = "accesscontrol.windows.net";
//        //
//        // Hosted app configuration
//        //
//        private static readonly string ClientId = string.IsNullOrEmpty(WebConfigurationManager.AppSettings.Get("ClientId")) ? WebConfigurationManager.AppSettings.Get("HostedAppName") : WebConfigurationManager.AppSettings.Get("ClientId");
//        private static readonly string IssuerId = string.IsNullOrEmpty(WebConfigurationManager.AppSettings.Get("IssuerId")) ? ClientId : WebConfigurationManager.AppSettings.Get("IssuerId");
//        private static readonly string HostedAppHostNameOverride = WebConfigurationManager.AppSettings.Get("HostedAppHostNameOverride");
//        private static readonly string HostedAppHostName = WebConfigurationManager.AppSettings.Get("HostedAppHostName");
//        private static readonly string ClientSecret = string.IsNullOrEmpty(WebConfigurationManager.AppSettings.Get("ClientSecret")) ? WebConfigurationManager.AppSettings.Get("HostedAppSigningKey") : WebConfigurationManager.AppSettings.Get("ClientSecret");
//        private static readonly string SecondaryClientSecret = WebConfigurationManager.AppSettings.Get("SecondaryClientSecret");
//        private static readonly string Realm = WebConfigurationManager.AppSettings.Get("Realm");
//        private static readonly string ServiceNamespace = WebConfigurationManager.AppSettings.Get("Realm");
//        private static readonly string ClientSigningCertificatePath = WebConfigurationManager.AppSettings.Get("ClientSigningCertificatePath");
//        private static readonly string ClientSigningCertificatePassword = WebConfigurationManager.AppSettings.Get("ClientSigningCertificatePassword");
//        private static readonly X509Certificate2 ClientCertificate = (string.IsNullOrEmpty(ClientSigningCertificatePath) || string.IsNullOrEmpty(ClientSigningCertificatePassword)) ? null : new X509Certificate2(ClientSigningCertificatePath, ClientSigningCertificatePassword);
//        private static readonly X509SigningCredentials SigningCredentials = (ClientCertificate == null) ? null : new X509SigningCredentials(ClientCertificate, SecurityAlgorithms.RsaSha256Signature, SecurityAlgorithms.Sha256Digest);
//        #endregion
//        #region private methods
//        private static ClientContext CreateAcsClientContextForUrl(SPRemoteEventProperties properties, Uri sharepointUrl)
//        {
//            string contextTokenString = properties.ContextToken;
//            if (String.IsNullOrEmpty(contextTokenString))
//            {
//                return null;
//            }
//            SharePointContextToken contextToken = ReadAndValidateContextToken(contextTokenString, OperationContext.Current.IncomingMessageHeaders.To.Host);
//            string accessToken = GetAccessToken(contextToken, sharepointUrl.Authority).AccessToken;
//            return GetClientContextWithAccessToken(sharepointUrl.ToString(), accessToken);
//        }
//        private static string GetAcsMetadataEndpointUrl()
//        {
//            return Path.Combine(GetAcsGlobalEndpointUrl(), AcsMetadataEndPointRelativeUrl);
//        }
//        private static string GetFormattedPrincipal(string principalName, string hostName, string realm)
//        {
//            if (!String.IsNullOrEmpty(hostName))
//            {
//                return String.Format(CultureInfo.InvariantCulture, "{0}/{1}@{2}", principalName, hostName, realm);
//            }
//            return String.Format(CultureInfo.InvariantCulture, "{0}@{1}", principalName, realm);
//        }
//        private static string GetAcsPrincipalName(string realm)
//        {
//            return GetFormattedPrincipal(AcsPrincipalName, new Uri(GetAcsGlobalEndpointUrl()).Host, realm);
//        }
//        private static string GetAcsGlobalEndpointUrl()
//        {
//            return String.Format(CultureInfo.InvariantCulture, "https://{0}.{1}/", GlobalEndPointPrefix, AcsHostUrl);
//        }
//        private static JsonWebSecurityTokenHandler CreateJsonWebSecurityTokenHandler()
//        {
//            JsonWebSecurityTokenHandler handler = new JsonWebSecurityTokenHandler();
//            handler.Configuration = new SecurityTokenHandlerConfiguration();
//            handler.Configuration.AudienceRestriction = new AudienceRestriction(AudienceUriMode.Never);
//            handler.Configuration.CertificateValidator = X509CertificateValidator.None;
//            List<byte[]> securityKeys = new List<byte[]>();
//            securityKeys.Add(Convert.FromBase64String(ClientSecret));
//            if (!string.IsNullOrEmpty(SecondaryClientSecret))
//            {
//                securityKeys.Add(Convert.FromBase64String(SecondaryClientSecret));
//            }
//            List<SecurityToken> securityTokens = new List<SecurityToken>();
//            securityTokens.Add(new MultipleSymmetricKeySecurityToken(securityKeys));
//            handler.Configuration.IssuerTokenResolver =
//                SecurityTokenResolver.CreateDefaultSecurityTokenResolver(
//                new ReadOnlyCollection<SecurityToken>(securityTokens),
//                false);
//            SymmetricKeyIssuerNameRegistry issuerNameRegistry = new SymmetricKeyIssuerNameRegistry();
//            foreach (byte[] securitykey in securityKeys)
//            {
//                issuerNameRegistry.AddTrustedIssuer(securitykey, GetAcsPrincipalName(ServiceNamespace));
//            }
//            handler.Configuration.IssuerNameRegistry = issuerNameRegistry;
//            return handler;
//        }
//        private static string GetS2SAccessTokenWithClaims(
//            string targetApplicationHostName,
//            string targetRealm,
//            IEnumerable<JsonWebTokenClaim> claims)
//        {
//            return IssueToken(
//                ClientId,
//                IssuerId,
//                targetRealm,
//                SharePointPrincipal,
//                targetRealm,
//                targetApplicationHostName,
//                true,
//                claims,
//                claims == null);
//        }
//        private static JsonWebTokenClaim[] GetClaimsWithWindowsIdentity(WindowsIdentity identity)
//        {
//            JsonWebTokenClaim[] claims = new JsonWebTokenClaim[]
//            {
//                new JsonWebTokenClaim(NameIdentifierClaimType, identity.User.Value.ToLower()),
//                new JsonWebTokenClaim("nii", "urn:office:idp:activedirectory")
//            };
//            return claims;
//        }
//        private static string IssueToken(
//            string sourceApplication,
//            string issuerApplication,
//            string sourceRealm,
//            string targetApplication,
//            string targetRealm,
//            string targetApplicationHostName,
//            bool trustedForDelegation,
//            IEnumerable<JsonWebTokenClaim> claims,
//            bool appOnly = false)
//        {
//            if (null == SigningCredentials)
//            {
//                throw new InvalidOperationException("SigningCredentials was not initialized");
//            }
//            #region Actor token
//            string issuer = string.IsNullOrEmpty(sourceRealm) ? issuerApplication : string.Format("{0}@{1}", issuerApplication, sourceRealm);
//            string nameid = string.IsNullOrEmpty(sourceRealm) ? sourceApplication : string.Format("{0}@{1}", sourceApplication, sourceRealm);
//            string audience = string.Format("{0}/{1}@{2}", targetApplication, targetApplicationHostName, targetRealm);
//            List<JsonWebTokenClaim> actorClaims = new List<JsonWebTokenClaim>();
//            actorClaims.Add(new JsonWebTokenClaim(JsonWebTokenConstants.ReservedClaims.NameIdentifier, nameid));
//            if (trustedForDelegation && !appOnly)
//            {
//                actorClaims.Add(new JsonWebTokenClaim(TrustedForImpersonationClaimType, "true"));
//            }
//            // Create token
//            JsonWebSecurityToken actorToken = new JsonWebSecurityToken(
//                issuer: issuer,
//                audience: audience,
//                validFrom: DateTime.UtcNow,
//                validTo: DateTime.UtcNow.Add(HighTrustAccessTokenLifetime),
//                signingCredentials: SigningCredentials,
//                claims: actorClaims);
//            string actorTokenString = new JsonWebSecurityTokenHandler().WriteTokenAsString(actorToken);
//            if (appOnly)
//            {
//                // App-only token is the same as actor token for delegated case
//                return actorTokenString;
//            }
//            #endregion Actor token
//            #region Outer token
//            List<JsonWebTokenClaim> outerClaims = null == claims ? new List<JsonWebTokenClaim>() : new List<JsonWebTokenClaim>(claims);
//            outerClaims.Add(new JsonWebTokenClaim(ActorTokenClaimType, actorTokenString));
//            JsonWebSecurityToken jsonToken = new JsonWebSecurityToken(
//                nameid, // outer token issuer should match actor token nameid
//                audience,
//                DateTime.UtcNow,
//                DateTime.UtcNow.Add(HighTrustAccessTokenLifetime),
//                outerClaims);
//            string accessToken = new JsonWebSecurityTokenHandler().WriteTokenAsString(jsonToken);
//            #endregion Outer token
//            return accessToken;
//        }
//        #endregion
//        #region AcsMetadataParser
//        // This class is used to get MetaData document from the global STS endpoint. It contains
//        // methods to parse the MetaData document and get endpoints and STS certificate.
//        public static class AcsMetadataParser
//        {
//            public static X509Certificate2 GetAcsSigningCert(string realm)
//            {
//                JsonMetadataDocument document = GetMetadataDocument(realm);
//                if (null != document.keys && document.keys.Count > 0)
//                {
//                    JsonKey signingKey = document.keys[0];
//                    if (null != signingKey && null != signingKey.keyValue)
//                    {
//                        return new X509Certificate2(Encoding.UTF8.GetBytes(signingKey.keyValue.value));
//                    }
//                }
//                throw new Exception("Metadata document does not contain ACS signing certificate.");
//            }
//            public static string GetDelegationServiceUrl(string realm)
//            {
//                JsonMetadataDocument document = GetMetadataDocument(realm);
//                JsonEndpoint delegationEndpoint = document.endpoints.SingleOrDefault(e => e.protocol == DelegationIssuance);
//                if (null != delegationEndpoint)
//                {
//                    return delegationEndpoint.location;
//                }
//                throw new Exception("Metadata document does not contain Delegation Service endpoint Url");
//            }
//            private static JsonMetadataDocument GetMetadataDocument(string realm)
//            {
//                string acsMetadataEndpointUrlWithRealm = String.Format(CultureInfo.InvariantCulture, "{0}?realm={1}",
//                                                                       GetAcsMetadataEndpointUrl(),
//                                                                       realm);
//                byte[] acsMetadata;
//                using (WebClient webClient = new WebClient())
//                {
//                    acsMetadata = webClient.DownloadData(acsMetadataEndpointUrlWithRealm);
//                }
//                string jsonResponseString = Encoding.UTF8.GetString(acsMetadata);
//                JavaScriptSerializer serializer = new JavaScriptSerializer();
//                JsonMetadataDocument document = serializer.Deserialize<JsonMetadataDocument>(jsonResponseString);
//                if (null == document)
//                {
//                    throw new Exception("No metadata document found at the global endpoint " + acsMetadataEndpointUrlWithRealm);
//                }
//                return document;
//            }
//            public static string GetStsUrl(string realm)
//            {
//                JsonMetadataDocument document = GetMetadataDocument(realm);
//                JsonEndpoint s2sEndpoint = document.endpoints.SingleOrDefault(e => e.protocol == S2SProtocol);
//                if (null != s2sEndpoint)
//                {
//                    return s2sEndpoint.location;
//                }
//                throw new Exception("Metadata document does not contain STS endpoint url");
//            }
//            private class JsonMetadataDocument
//            {
//                public string serviceName { get; set; }
//                public List<JsonEndpoint> endpoints { get; set; }
//                public List<JsonKey> keys { get; set; }
//            }
//            private class JsonEndpoint
//            {
//                public string location { get; set; }
//                public string protocol { get; set; }
//                public string usage { get; set; }
//            }
//            private class JsonKeyValue
//            {
//                public string type { get; set; }
//                public string value { get; set; }
//            }
//            private class JsonKey
//            {
//                public string usage { get; set; }
//                public JsonKeyValue keyValue { get; set; }
//            }
//        }
//        #endregion
//    }
//    /// <summary>
//    /// A JsonWebSecurityToken generated by SharePoint to authenticate to a 3rd party application and allow callbacks using a refresh token
//    /// </summary>
//    public class SharePointContextToken : JsonWebSecurityToken
//    {
//        public static SharePointContextToken Create(JsonWebSecurityToken contextToken)
//        {
//            return new SharePointContextToken(contextToken.Issuer, contextToken.Audience, contextToken.ValidFrom, contextToken.ValidTo, contextToken.Claims);
//        }
//        public SharePointContextToken(string issuer, string audience, DateTime validFrom, DateTime validTo, IEnumerable<JsonWebTokenClaim> claims)
//            : base(issuer, audience, validFrom, validTo, claims)
//        {
//        }
//        public SharePointContextToken(string issuer, string audience, DateTime validFrom, DateTime validTo, IEnumerable<JsonWebTokenClaim> claims, SecurityToken issuerToken, JsonWebSecurityToken actorToken)
//            : base(issuer, audience, validFrom, validTo, claims, issuerToken, actorToken)
//        {
//        }
//        public SharePointContextToken(string issuer, string audience, DateTime validFrom, DateTime validTo, IEnumerable<JsonWebTokenClaim> claims, SigningCredentials signingCredentials)
//            : base(issuer, audience, validFrom, validTo, claims, signingCredentials)
//        {
//        }
//        public string NameId
//        {
//            get
//            {
//                return GetClaimValue(this, "nameid");
//            }
//        }
//        /// <summary>
//        /// The principal name portion of the context token's "appctxsender" claim
//        /// </summary>
//        public string TargetPrincipalName
//        {
//            get
//            {
//                string appctxsender = GetClaimValue(this, "appctxsender");
//                if (appctxsender == null)
//                {
//                    return null;
//                }
//                return appctxsender.Split('@')[0];
//            }
//        }
//        /// <summary>
//        /// The context token's "refreshtoken" claim
//        /// </summary>
//        public string RefreshToken
//        {
//            get
//            {
//                return GetClaimValue(this, "refreshtoken");
//            }
//        }
//        /// <summary>
//        /// The context token's "CacheKey" claim
//        /// </summary>
//        public string CacheKey
//        {
//            get
//            {
//                string appctx = GetClaimValue(this, "appctx");
//                if (appctx == null)
//                {
//                    return null;
//                }
//                ClientContext ctx = new ClientContext("http://tempuri.org");
//                Dictionary<string, object> dict = (Dictionary<string, object>)ctx.ParseObjectFromJsonString(appctx);
//                string cacheKey = (string)dict["CacheKey"];
//                return cacheKey;
//            }
//        }
//        /// <summary>
//        /// The context token's "SecurityTokenServiceUri" claim
//        /// </summary>
//        public string SecurityTokenServiceUri
//        {
//            get
//            {
//                string appctx = GetClaimValue(this, "appctx");
//                if (appctx == null)
//                {
//                    return null;
//                }
//                ClientContext ctx = new ClientContext("http://tempuri.org");
//                Dictionary<string, object> dict = (Dictionary<string, object>)ctx.ParseObjectFromJsonString(appctx);
//                string securityTokenServiceUri = (string)dict["SecurityTokenServiceUri"];
//                return securityTokenServiceUri;
//            }
//        }
//        /// <summary>
//        /// The realm portion of the context token's "audience" claim
//        /// </summary>
//        public string Realm
//        {
//            get
//            {
//                string aud = Audience;
//                if (aud == null)
//                {
//                    return null;
//                }
//                string tokenRealm = aud.Substring(aud.IndexOf('@') + 1);
//                return tokenRealm;
//            }
//        }
//        private static string GetClaimValue(JsonWebSecurityToken token, string claimType)
//        {
//            if (token == null)
//            {
//                throw new ArgumentNullException("token");
//            }
//            foreach (JsonWebTokenClaim claim in token.Claims)
//            {
//                if (StringComparer.Ordinal.Equals(claim.ClaimType, claimType))
//                {
//                    return claim.Value;
//                }
//            }
//            return null;
//        }
//    }
//    /// <summary>
//    /// Represents a security token which contains multiple security keys that are generated using symmetric algorithms.
//    /// </summary>
//    public class MultipleSymmetricKeySecurityToken : SecurityToken
//    {
//        /// <summary>
//        /// Initializes a new instance of the MultipleSymmetricKeySecurityToken class.
//        /// </summary>
//        /// <param name="keys">An enumeration of Byte arrays that contain the symmetric keys.</param>
//        public MultipleSymmetricKeySecurityToken(IEnumerable<byte[]> keys)
//            : this(UniqueId.CreateUniqueId(), keys)
//        {
//        }
//        /// <summary>
//        /// Initializes a new instance of the MultipleSymmetricKeySecurityToken class.
//        /// </summary>
//        /// <param name="tokenId">The unique identifier of the security token.</param>
//        /// <param name="keys">An enumeration of Byte arrays that contain the symmetric keys.</param>
//        public MultipleSymmetricKeySecurityToken(string tokenId, IEnumerable<byte[]> keys)
//        {
//            if (keys == null)
//            {
//                throw new ArgumentNullException("keys");
//            }
//            if (String.IsNullOrEmpty(tokenId))
//            {
//                throw new ArgumentException("Value cannot be a null or empty string.", "tokenId");
//            }
//            foreach (byte[] key in keys)
//            {
//                if (key.Length <= 0)
//                {
//                    throw new ArgumentException("The key length must be greater then zero.", "keys");
//                }
//            }
//            id = tokenId;
//            effectiveTime = DateTime.UtcNow;
//            securityKeys = CreateSymmetricSecurityKeys(keys);
//        }
//        /// <summary>
//        /// Gets the unique identifier of the security token.
//        /// </summary>
//        public override string Id
//        {
//            get
//            {
//                return id;
//            }
//        }
//        /// <summary>
//        /// Gets the cryptographic keys associated with the security token.
//        /// </summary>
//        public override ReadOnlyCollection<SecurityKey> SecurityKeys
//        {
//            get
//            {
//                return securityKeys.AsReadOnly();
//            }
//        }
//        /// <summary>
//        /// Gets the first instant in time at which this security token is valid.
//        /// </summary>
//        public override DateTime ValidFrom
//        {
//            get
//            {
//                return effectiveTime;
//            }
//        }
//        /// <summary>
//        /// Gets the last instant in time at which this security token is valid.
//        /// </summary>
//        public override DateTime ValidTo
//        {
//            get
//            {
//                // Never expire
//                return DateTime.MaxValue;
//            }
//        }
//        /// <summary>
//        /// Returns a value that indicates whether the key identifier for this instance can be resolved to the specified key identifier.
//        /// </summary>
//        /// <param name="keyIdentifierClause">A SecurityKeyIdentifierClause to compare to this instance</param>
//        /// <returns>true if keyIdentifierClause is a SecurityKeyIdentifierClause and it has the same unique identifier as the Id property; otherwise, false.</returns>
//        public override bool MatchesKeyIdentifierClause(SecurityKeyIdentifierClause keyIdentifierClause)
//        {
//            if (keyIdentifierClause == null)
//            {
//                throw new ArgumentNullException("keyIdentifierClause");
//            }
//            // Since this is a symmetric token and we do not have IDs to distinguish tokens, we just check for the
//            // presence of a SymmetricIssuerKeyIdentifier. The actual mapping to the issuer takes place later
//            // when the key is matched to the issuer.
//            if (keyIdentifierClause is SymmetricIssuerKeyIdentifierClause)
//            {
//                return true;
//            }
//            return base.MatchesKeyIdentifierClause(keyIdentifierClause);
//        }
//        #region private members
//        private List<SecurityKey> CreateSymmetricSecurityKeys(IEnumerable<byte[]> keys)
//        {
//            List<SecurityKey> symmetricKeys = new List<SecurityKey>();
//            foreach (byte[] key in keys)
//            {
//                symmetricKeys.Add(new InMemorySymmetricSecurityKey(key));
//            }
//            return symmetricKeys;
//        }
//        private string id;
//        private DateTime effectiveTime;
//        private List<SecurityKey> securityKeys;
//        #endregion
//    }
//}
//namespace Microshaoft.SharePointApps
//{
//    using Microsoft.IdentityModel.S2S.Protocols.OAuth2;
//    using Microsoft.IdentityModel.Tokens;
//    using Microsoft.SharePoint.Client;
//    using System;
//    using System.Net;
//    using System.Security.Principal;
//    using System.Web;
//    using System.Web.Configuration;
//    /// <summary>
//    /// Encapsulates all the information from SharePoint.
//    /// </summary>
//    public abstract class SharePointContext
//    {
//        public const string SPHostUrlKey = "SPHostUrl";
//        public const string SPAppWebUrlKey = "SPAppWebUrl";
//        public const string SPLanguageKey = "SPLanguage";
//        public const string SPClientTagKey = "SPClientTag";
//        public const string SPProductNumberKey = "SPProductNumber";
//        protected static readonly TimeSpan AccessTokenLifetimeTolerance = TimeSpan.FromMinutes(5.0);
//        private readonly Uri spHostUrl;
//        private readonly Uri spAppWebUrl;
//        private readonly string spLanguage;
//        private readonly string spClientTag;
//        private readonly string spProductNumber;
//        // <AccessTokenString, UtcExpiresOn>
//        protected Tuple<string, DateTime> userAccessTokenForSPHost;
//        protected Tuple<string, DateTime> userAccessTokenForSPAppWeb;
//        protected Tuple<string, DateTime> appOnlyAccessTokenForSPHost;
//        protected Tuple<string, DateTime> appOnlyAccessTokenForSPAppWeb;
//        /// <summary>
//        /// Gets the SharePoint host url from QueryString of the specified HTTP request.
//        /// </summary>
//        /// <param name="httpRequest">The specified HTTP request.</param>
//        /// <returns>The SharePoint host url. Returns <c>null</c> if the HTTP request doesn't contain the SharePoint host url.</returns>
//        public static Uri GetSPHostUrl(HttpRequestBase httpRequest)
//        {
//            if (httpRequest == null)
//            {
//                throw new ArgumentNullException("httpRequest");
//            }
//            string spHostUrlString = TokenHelper.EnsureTrailingSlash(httpRequest.QueryString[SPHostUrlKey]);
//            Uri spHostUrl;
//            if (Uri.TryCreate(spHostUrlString, UriKind.Absolute, out spHostUrl) &&
//                (spHostUrl.Scheme == Uri.UriSchemeHttp || spHostUrl.Scheme == Uri.UriSchemeHttps))
//            {
//                return spHostUrl;
//            }
//            return null;
//        }
//        /// <summary>
//        /// Gets the SharePoint host url from QueryString of the specified HTTP request.
//        /// </summary>
//        /// <param name="httpRequest">The specified HTTP request.</param>
//        /// <returns>The SharePoint host url. Returns <c>null</c> if the HTTP request doesn't contain the SharePoint host url.</returns>
//        public static Uri GetSPHostUrl(HttpRequest httpRequest)
//        {
//            return GetSPHostUrl(new HttpRequestWrapper(httpRequest));
//        }
//        /// <summary>
//        /// The SharePoint host url.
//        /// </summary>
//        public Uri SPHostUrl
//        {
//            get { return this.spHostUrl; }
//        }
//        /// <summary>
//        /// The SharePoint app web url.
//        /// </summary>
//        public Uri SPAppWebUrl
//        {
//            get { return this.spAppWebUrl; }
//        }
//        /// <summary>
//        /// The SharePoint language.
//        /// </summary>
//        public string SPLanguage
//        {
//            get { return this.spLanguage; }
//        }
//        /// <summary>
//        /// The SharePoint client tag.
//        /// </summary>
//        public string SPClientTag
//        {
//            get { return this.spClientTag; }
//        }
//        /// <summary>
//        /// The SharePoint product number.
//        /// </summary>
//        public string SPProductNumber
//        {
//            get { return this.spProductNumber; }
//        }
//        /// <summary>
//        /// The user access token for the SharePoint host.
//        /// </summary>
//        public abstract string UserAccessTokenForSPHost
//        {
//            get;
//        }
//        /// <summary>
//        /// The user access token for the SharePoint app web.
//        /// </summary>
//        public abstract string UserAccessTokenForSPAppWeb
//        {
//            get;
//        }
//        /// <summary>
//        /// The app only access token for the SharePoint host.
//        /// </summary>
//        public abstract string AppOnlyAccessTokenForSPHost
//        {
//            get;
//        }
//        /// <summary>
//        /// The app only access token for the SharePoint app web.
//        /// </summary>
//        public abstract string AppOnlyAccessTokenForSPAppWeb
//        {
//            get;
//        }
//        /// <summary>
//        /// Constructor.
//        /// </summary>
//        /// <param name="spHostUrl">The SharePoint host url.</param>
//        /// <param name="spAppWebUrl">The SharePoint app web url.</param>
//        /// <param name="spLanguage">The SharePoint language.</param>
//        /// <param name="spClientTag">The SharePoint client tag.</param>
//        /// <param name="spProductNumber">The SharePoint product number.</param>
//        protected SharePointContext(Uri spHostUrl, Uri spAppWebUrl, string spLanguage, string spClientTag, string spProductNumber)
//        {
//            if (spHostUrl == null)
//            {
//                throw new ArgumentNullException("spHostUrl");
//            }
//            if (string.IsNullOrEmpty(spLanguage))
//            {
//                throw new ArgumentNullException("spLanguage");
//            }
//            if (string.IsNullOrEmpty(spClientTag))
//            {
//                throw new ArgumentNullException("spClientTag");
//            }
//            if (string.IsNullOrEmpty(spProductNumber))
//            {
//                throw new ArgumentNullException("spProductNumber");
//            }
//            this.spHostUrl = spHostUrl;
//            this.spAppWebUrl = spAppWebUrl;
//            this.spLanguage = spLanguage;
//            this.spClientTag = spClientTag;
//            this.spProductNumber = spProductNumber;
//        }
//        /// <summary>
//        /// Creates a user ClientContext for the SharePoint host.
//        /// </summary>
//        /// <returns>A ClientContext instance.</returns>
//        public ClientContext CreateUserClientContextForSPHost()
//        {
//            return CreateClientContext(this.SPHostUrl, this.UserAccessTokenForSPHost);
//        }
//        /// <summary>
//        /// Creates a user ClientContext for the SharePoint app web.
//        /// </summary>
//        /// <returns>A ClientContext instance.</returns>
//        public ClientContext CreateUserClientContextForSPAppWeb()
//        {
//            return CreateClientContext(this.SPAppWebUrl, this.UserAccessTokenForSPAppWeb);
//        }
//        /// <summary>
//        /// Creates app only ClientContext for the SharePoint host.
//        /// </summary>
//        /// <returns>A ClientContext instance.</returns>
//        public ClientContext CreateAppOnlyClientContextForSPHost()
//        {
//            return CreateClientContext(this.SPHostUrl, this.AppOnlyAccessTokenForSPHost);
//        }
//        /// <summary>
//        /// Creates an app only ClientContext for the SharePoint app web.
//        /// </summary>
//        /// <returns>A ClientContext instance.</returns>
//        public ClientContext CreateAppOnlyClientContextForSPAppWeb()
//        {
//            return CreateClientContext(this.SPAppWebUrl, this.AppOnlyAccessTokenForSPAppWeb);
//        }
//        /// <summary>
//        /// Gets the database connection string from SharePoint for autohosted app.
//        /// </summary>
//        /// <returns>The database connection string. Returns <c>null</c> if the app is not autohosted or there is no database.</returns>
//        public string GetDatabaseConnectionString()
//        {
//            string dbConnectionString = null;
//            using (ClientContext clientContext = CreateAppOnlyClientContextForSPHost())
//            {
//                if (clientContext != null)
//                {
//                    var result = AppInstance.RetrieveAppDatabaseConnectionString(clientContext);
//                    clientContext.ExecuteQuery();
//                    dbConnectionString = result.Value;
//                }
//            }
//            if (dbConnectionString == null)
//            {
//                const string LocalDBInstanceForDebuggingKey = "LocalDBInstanceForDebugging";
//                var dbConnectionStringSettings = WebConfigurationManager.ConnectionStrings[LocalDBInstanceForDebuggingKey];
//                dbConnectionString = dbConnectionStringSettings != null ? dbConnectionStringSettings.ConnectionString : null;
//            }
//            return dbConnectionString;
//        }
//        /// <summary>
//        /// Determines if the specified access token is valid.
//        /// It considers an access token as not valid if it is null, or it has expired.
//        /// </summary>
//        /// <param name="accessToken">The access token to verify.</param>
//        /// <returns>True if the access token is valid.</returns>
//        protected static bool IsAccessTokenValid(Tuple<string, DateTime> accessToken)
//        {
//            return accessToken != null &&
//                   !string.IsNullOrEmpty(accessToken.Item1) &&
//                   accessToken.Item2 > DateTime.UtcNow;
//        }
//        /// <summary>
//        /// Creates a ClientContext with the specified SharePoint site url and the access token.
//        /// </summary>
//        /// <param name="spSiteUrl">The site url.</param>
//        /// <param name="accessToken">The access token.</param>
//        /// <returns>A ClientContext instance.</returns>
//        private static ClientContext CreateClientContext(Uri spSiteUrl, string accessToken)
//        {
//            if (spSiteUrl != null && !string.IsNullOrEmpty(accessToken))
//            {
//                return TokenHelper.GetClientContextWithAccessToken(spSiteUrl.AbsoluteUri, accessToken);
//            }
//            return null;
//        }
//    }
//    /// <summary>
//    /// Redirection status.
//    /// </summary>
//    public enum RedirectionStatus
//    {
//        Ok,
//        ShouldRedirect,
//        CanNotRedirect
//    }
//    /// <summary>
//    /// Provides SharePointContext instances.
//    /// </summary>
//    public abstract class SharePointContextProvider
//    {
//        private static SharePointContextProvider current;
//        /// <summary>
//        /// The current SharePointContextProvider instance.
//        /// </summary>
//        public static SharePointContextProvider Current
//        {
//            get { return SharePointContextProvider.current; }
//        }
//        /// <summary>
//        /// Initializes the default SharePointContextProvider instance.
//        /// </summary>
//        static SharePointContextProvider()
//        {
//            if (!TokenHelper.IsHighTrustApp())
//            {
//                SharePointContextProvider.current = new SharePointAcsContextProvider();
//            }
//            else
//            {
//                SharePointContextProvider.current = new SharePointHighTrustContextProvider();
//            }
//        }
//        /// <summary>
//        /// Registers the specified SharePointContextProvider instance as current.
//        /// It should be called by Application_Start() in Global.asax.
//        /// </summary>
//        /// <param name="provider">The SharePointContextProvider to be set as current.</param>
//        public static void Register(SharePointContextProvider provider)
//        {
//            if (provider == null)
//            {
//                throw new ArgumentNullException("provider");
//            }
//            SharePointContextProvider.current = provider;
//        }
//        /// <summary>
//        /// Checks if it is necessary to redirect to SharePoint for user to authenticate.
//        /// </summary>
//        /// <param name="httpContext">The HTTP context.</param>
//        /// <param name="redirectUrl">The redirect url to SharePoint if the status is ShouldRedirect. <c>Null</c> if the status is Ok or CanNotRedirect.</param>
//        /// <returns>Redirection status.</returns>
//        public static RedirectionStatus CheckRedirectionStatus(HttpContextBase httpContext, out Uri redirectUrl)
//        {
//            if (httpContext == null)
//            {
//                throw new ArgumentNullException("httpContext");
//            }
//            redirectUrl = null;
//            if (SharePointContextProvider.Current.GetSharePointContext(httpContext) != null)
//            {
//                return RedirectionStatus.Ok;
//            }
//            const string SPHasRedirectedToSharePointKey = "SPHasRedirectedToSharePoint";
//            if (!string.IsNullOrEmpty(httpContext.Request.QueryString[SPHasRedirectedToSharePointKey]))
//            {
//                return RedirectionStatus.CanNotRedirect;
//            }
//            Uri spHostUrl = SharePointContext.GetSPHostUrl(httpContext.Request);
//            if (spHostUrl == null)
//            {
//                return RedirectionStatus.CanNotRedirect;
//            }
//            if (StringComparer.OrdinalIgnoreCase.Equals(httpContext.Request.HttpMethod, "POST"))
//            {
//                return RedirectionStatus.CanNotRedirect;
//            }
//            Uri requestUrl = httpContext.Request.Url;
//            var queryNameValueCollection = HttpUtility.ParseQueryString(requestUrl.Query);
//            // Removes the values that are included in {StandardTokens}, as {StandardTokens} will be inserted at the beginning of the query string.
//            queryNameValueCollection.Remove(SharePointContext.SPHostUrlKey);
//            queryNameValueCollection.Remove(SharePointContext.SPAppWebUrlKey);
//            queryNameValueCollection.Remove(SharePointContext.SPLanguageKey);
//            queryNameValueCollection.Remove(SharePointContext.SPClientTagKey);
//            queryNameValueCollection.Remove(SharePointContext.SPProductNumberKey);
//            // Adds SPHasRedirectedToSharePoint=1.
//            queryNameValueCollection.Add(SPHasRedirectedToSharePointKey, "1");
//            UriBuilder returnUrlBuilder = new UriBuilder(requestUrl);
//            returnUrlBuilder.Query = queryNameValueCollection.ToString();
//            // Inserts StandardTokens.
//            const string StandardTokens = "{StandardTokens}";
//            string returnUrlString = returnUrlBuilder.Uri.AbsoluteUri;
//            returnUrlString = returnUrlString.Insert(returnUrlString.IndexOf("?") + 1, StandardTokens + "&");
//            // Constructs redirect url.
//            string redirectUrlString = TokenHelper.GetAppContextTokenRequestUrl(spHostUrl.AbsoluteUri, Uri.EscapeDataString(returnUrlString));
//            redirectUrl = new Uri(redirectUrlString, UriKind.Absolute);
//            return RedirectionStatus.ShouldRedirect;
//        }
//        /// <summary>
//        /// Checks if it is necessary to redirect to SharePoint for user to authenticate.
//        /// </summary>
//        /// <param name="httpContext">The HTTP context.</param>
//        /// <param name="redirectUrl">The redirect url to SharePoint if the status is ShouldRedirect. <c>Null</c> if the status is Ok or CanNotRedirect.</param>
//        /// <returns>Redirection status.</returns>
//        public static RedirectionStatus CheckRedirectionStatus(HttpContext httpContext, out Uri redirectUrl)
//        {
//            return CheckRedirectionStatus(new HttpContextWrapper(httpContext), out redirectUrl);
//        }
//        /// <summary>
//        /// Creates a SharePointContext instance with the specified HTTP request.
//        /// </summary>
//        /// <param name="httpRequest">The HTTP request.</param>
//        /// <returns>The SharePointContext instance. Returns <c>null</c> if errors occur.</returns>
//        public SharePointContext CreateSharePointContext(HttpRequestBase httpRequest)
//        {
//            if (httpRequest == null)
//            {
//                throw new ArgumentNullException("httpRequest");
//            }
//            // SPHostUrl
//            Uri spHostUrl = SharePointContext.GetSPHostUrl(httpRequest);
//            if (spHostUrl == null)
//            {
//                return null;
//            }
//            // SPAppWebUrl
//            string spAppWebUrlString = TokenHelper.EnsureTrailingSlash(httpRequest.QueryString[SharePointContext.SPAppWebUrlKey]);
//            Uri spAppWebUrl;
//            if (!Uri.TryCreate(spAppWebUrlString, UriKind.Absolute, out spAppWebUrl) ||
//                !(spAppWebUrl.Scheme == Uri.UriSchemeHttp || spAppWebUrl.Scheme == Uri.UriSchemeHttps))
//            {
//                spAppWebUrl = null;
//            }
//            // SPLanguage
//            string spLanguage = httpRequest.QueryString[SharePointContext.SPLanguageKey];
//            if (string.IsNullOrEmpty(spLanguage))
//            {
//                return null;
//            }
//            // SPClientTag
//            string spClientTag = httpRequest.QueryString[SharePointContext.SPClientTagKey];
//            if (string.IsNullOrEmpty(spClientTag))
//            {
//                return null;
//            }
//            // SPProductNumber
//            string spProductNumber = httpRequest.QueryString[SharePointContext.SPProductNumberKey];
//            if (string.IsNullOrEmpty(spProductNumber))
//            {
//                return null;
//            }
//            return CreateSharePointContext(spHostUrl, spAppWebUrl, spLanguage, spClientTag, spProductNumber, httpRequest);
//        }
//        /// <summary>
//        /// Creates a SharePointContext instance with the specified HTTP request.
//        /// </summary>
//        /// <param name="httpRequest">The HTTP request.</param>
//        /// <returns>The SharePointContext instance. Returns <c>null</c> if errors occur.</returns>
//        public SharePointContext CreateSharePointContext(HttpRequest httpRequest)
//        {
//            return CreateSharePointContext(new HttpRequestWrapper(httpRequest));
//        }
//        /// <summary>
//        /// Gets a SharePointContext instance associated with the specified HTTP context.
//        /// </summary>
//        /// <param name="httpContext">The HTTP context.</param>
//        /// <returns>The SharePointContext instance. Returns <c>null</c> if not found and a new instance can't be created.</returns>
//        public SharePointContext GetSharePointContext(HttpContextBase httpContext)
//        {
//            if (httpContext == null)
//            {
//                throw new ArgumentNullException("httpContext");
//            }
//            Uri spHostUrl = SharePointContext.GetSPHostUrl(httpContext.Request);
//            if (spHostUrl == null)
//            {
//                return null;
//            }
//            SharePointContext spContext = LoadSharePointContext(httpContext);
//            if (spContext == null || !ValidateSharePointContext(spContext, httpContext))
//            {
//                spContext = CreateSharePointContext(httpContext.Request);
//                if (spContext != null)
//                {
//                    SaveSharePointContext(spContext, httpContext);
//                }
//            }
//            return spContext;
//        }
//        /// <summary>
//        /// Gets a SharePointContext instance associated with the specified HTTP context.
//        /// </summary>
//        /// <param name="httpContext">The HTTP context.</param>
//        /// <returns>The SharePointContext instance. Returns <c>null</c> if not found and a new instance can't be created.</returns>
//        public SharePointContext GetSharePointContext(HttpContext httpContext)
//        {
//            return GetSharePointContext(new HttpContextWrapper(httpContext));
//        }
//        /// <summary>
//        /// Creates a SharePointContext instance.
//        /// </summary>
//        /// <param name="spHostUrl">The SharePoint host url.</param>
//        /// <param name="spAppWebUrl">The SharePoint app web url.</param>
//        /// <param name="spLanguage">The SharePoint language.</param>
//        /// <param name="spClientTag">The SharePoint client tag.</param>
//        /// <param name="spProductNumber">The SharePoint product number.</param>
//        /// <param name="httpRequest">The HTTP request.</param>
//        /// <returns>The SharePointContext instance. Returns <c>null</c> if errors occur.</returns>
//        protected abstract SharePointContext CreateSharePointContext(Uri spHostUrl, Uri spAppWebUrl, string spLanguage, string spClientTag, string spProductNumber, HttpRequestBase httpRequest);
//        /// <summary>
//        /// Validates if the given SharePointContext can be used with the specified HTTP context.
//        /// </summary>
//        /// <param name="spContext">The SharePointContext.</param>
//        /// <param name="httpContext">The HTTP context.</param>
//        /// <returns>True if the given SharePointContext can be used with the specified HTTP context.</returns>
//        protected abstract bool ValidateSharePointContext(SharePointContext spContext, HttpContextBase httpContext);
//        /// <summary>
//        /// Loads the SharePointContext instance associated with the specified HTTP context.
//        /// </summary>
//        /// <param name="httpContext">The HTTP context.</param>
//        /// <returns>The SharePointContext instance. Returns <c>null</c> if not found.</returns>
//        protected abstract SharePointContext LoadSharePointContext(HttpContextBase httpContext);
//        /// <summary>
//        /// Saves the specified SharePointContext instance associated with the specified HTTP context.
//        /// <c>null</c> is accepted for clearing the SharePointContext instance associated with the HTTP context.
//        /// </summary>
//        /// <param name="spContext">The SharePointContext instance to be saved, or <c>null</c>.</param>
//        /// <param name="httpContext">The HTTP context.</param>
//        protected abstract void SaveSharePointContext(SharePointContext spContext, HttpContextBase httpContext);
//    }
//    #region ACS
//    /// <summary>
//    /// Encapsulates all the information from SharePoint in ACS mode.
//    /// </summary>
//    public class SharePointAcsContext : SharePointContext
//    {
//        private readonly string contextToken;
//        private readonly SharePointContextToken contextTokenObj;
//        /// <summary>
//        /// The context token.
//        /// </summary>
//        public string ContextToken
//        {
//            get { return this.contextTokenObj.ValidTo > DateTime.UtcNow ? this.contextToken : null; }
//        }
//        /// <summary>
//        /// The context token's "CacheKey" claim.
//        /// </summary>
//        public string CacheKey
//        {
//            get { return this.contextTokenObj.ValidTo > DateTime.UtcNow ? this.contextTokenObj.CacheKey : null; }
//        }
//        /// <summary>
//        /// The context token's "refreshtoken" claim.
//        /// </summary>
//        public string RefreshToken
//        {
//            get { return this.contextTokenObj.ValidTo > DateTime.UtcNow ? this.contextTokenObj.RefreshToken : null; }
//        }
//        public override string UserAccessTokenForSPHost
//        {
//            get
//            {
//                return GetAccessTokenString(ref this.userAccessTokenForSPHost,
//                                            () => TokenHelper.GetAccessToken(this.contextTokenObj, this.SPHostUrl.Authority));
//            }
//        }
//        public override string UserAccessTokenForSPAppWeb
//        {
//            get
//            {
//                if (this.SPAppWebUrl == null)
//                {
//                    return null;
//                }
//                return GetAccessTokenString(ref this.userAccessTokenForSPAppWeb,
//                                            () => TokenHelper.GetAccessToken(this.contextTokenObj, this.SPAppWebUrl.Authority));
//            }
//        }
//        public override string AppOnlyAccessTokenForSPHost
//        {
//            get
//            {
//                return GetAccessTokenString(ref this.appOnlyAccessTokenForSPHost,
//                                            () => TokenHelper.GetAppOnlyAccessToken(TokenHelper.SharePointPrincipal, this.SPHostUrl.Authority, TokenHelper.GetRealmFromTargetUrl(this.SPHostUrl)));
//            }
//        }
//        public override string AppOnlyAccessTokenForSPAppWeb
//        {
//            get
//            {
//                if (this.SPAppWebUrl == null)
//                {
//                    return null;
//                }
//                return GetAccessTokenString(ref this.appOnlyAccessTokenForSPAppWeb,
//                                            () => TokenHelper.GetAppOnlyAccessToken(TokenHelper.SharePointPrincipal, this.SPAppWebUrl.Authority, TokenHelper.GetRealmFromTargetUrl(this.SPAppWebUrl)));
//            }
//        }
//        public SharePointAcsContext(Uri spHostUrl, Uri spAppWebUrl, string spLanguage, string spClientTag, string spProductNumber, string contextToken, SharePointContextToken contextTokenObj)
//            : base(spHostUrl, spAppWebUrl, spLanguage, spClientTag, spProductNumber)
//        {
//            if (string.IsNullOrEmpty(contextToken))
//            {
//                throw new ArgumentNullException("contextToken");
//            }
//            if (contextTokenObj == null)
//            {
//                throw new ArgumentNullException("contextTokenObj");
//            }
//            this.contextToken = contextToken;
//            this.contextTokenObj = contextTokenObj;
//        }
//        /// <summary>
//        /// Ensures the access token is valid and returns it.
//        /// </summary>
//        /// <param name="accessToken">The access token to verify.</param>
//        /// <param name="tokenRenewalHandler">The token renewal handler.</param>
//        /// <returns>The access token string.</returns>
//        private static string GetAccessTokenString(ref Tuple<string, DateTime> accessToken, Func<OAuth2AccessTokenResponse> tokenRenewalHandler)
//        {
//            RenewAccessTokenIfNeeded(ref accessToken, tokenRenewalHandler);
//            return IsAccessTokenValid(accessToken) ? accessToken.Item1 : null;
//        }
//        /// <summary>
//        /// Renews the access token if it is not valid.
//        /// </summary>
//        /// <param name="accessToken">The access token to renew.</param>
//        /// <param name="tokenRenewalHandler">The token renewal handler.</param>
//        private static void RenewAccessTokenIfNeeded(ref Tuple<string, DateTime> accessToken, Func<OAuth2AccessTokenResponse> tokenRenewalHandler)
//        {
//            if (IsAccessTokenValid(accessToken))
//            {
//                return;
//            }
//            try
//            {
//                OAuth2AccessTokenResponse oAuth2AccessTokenResponse = tokenRenewalHandler();
//                DateTime expiresOn = oAuth2AccessTokenResponse.ExpiresOn;
//                if ((expiresOn - oAuth2AccessTokenResponse.NotBefore) > AccessTokenLifetimeTolerance)
//                {
//                    // Make the access token get renewed a bit earlier than the time when it expires
//                    // so that the calls to SharePoint with it will have enough time to complete successfully.
//                    expiresOn -= AccessTokenLifetimeTolerance;
//                }
//                accessToken = Tuple.Create(oAuth2AccessTokenResponse.AccessToken, expiresOn);
//            }
//            catch (WebException)
//            {
//            }
//        }
//    }
//    /// <summary>
//    /// Default provider for SharePointAcsContext.
//    /// </summary>
//    public class SharePointAcsContextProvider : SharePointContextProvider
//    {
//        private const string SPContextKey = "SPContext";
//        private const string SPCacheKeyKey = "SPCacheKey";
//        protected override SharePointContext CreateSharePointContext(Uri spHostUrl, Uri spAppWebUrl, string spLanguage, string spClientTag, string spProductNumber, HttpRequestBase httpRequest)
//        {
//            string contextTokenString = TokenHelper.GetContextTokenFromRequest(httpRequest);
//            if (string.IsNullOrEmpty(contextTokenString))
//            {
//                return null;
//            }
//            SharePointContextToken contextToken = null;
//            try
//            {
//                contextToken = TokenHelper.ReadAndValidateContextToken(contextTokenString, httpRequest.Url.Authority);
//            }
//            catch (WebException)
//            {
//                return null;
//            }
//            catch (AudienceUriValidationFailedException)
//            {
//                return null;
//            }
//            return new SharePointAcsContext(spHostUrl, spAppWebUrl, spLanguage, spClientTag, spProductNumber, contextTokenString, contextToken);
//        }
//        protected override bool ValidateSharePointContext(SharePointContext spContext, HttpContextBase httpContext)
//        {
//            SharePointAcsContext spAcsContext = spContext as SharePointAcsContext;
//            if (spAcsContext != null)
//            {
//                Uri spHostUrl = SharePointContext.GetSPHostUrl(httpContext.Request);
//                string contextToken = TokenHelper.GetContextTokenFromRequest(httpContext.Request);
//                HttpCookie spCacheKeyCookie = httpContext.Request.Cookies[SPCacheKeyKey];
//                string spCacheKey = spCacheKeyCookie != null ? spCacheKeyCookie.Value : null;
//                return spHostUrl == spAcsContext.SPHostUrl &&
//                       !string.IsNullOrEmpty(spAcsContext.CacheKey) &&
//                       spCacheKey == spAcsContext.CacheKey &&
//                       !string.IsNullOrEmpty(spAcsContext.ContextToken) &&
//                       (string.IsNullOrEmpty(contextToken) || contextToken == spAcsContext.ContextToken);
//            }
//            return false;
//        }
//        protected override SharePointContext LoadSharePointContext(HttpContextBase httpContext)
//        {
//            return httpContext.Session[SPContextKey] as SharePointAcsContext;
//        }
//        protected override void SaveSharePointContext(SharePointContext spContext, HttpContextBase httpContext)
//        {
//            SharePointAcsContext spAcsContext = spContext as SharePointAcsContext;
//            if (spAcsContext != null)
//            {
//                HttpCookie spCacheKeyCookie = new HttpCookie(SPCacheKeyKey)
//                {
//                    Value = spAcsContext.CacheKey,
//                    Secure = true,
//                    HttpOnly = true
//                };
//                httpContext.Response.AppendCookie(spCacheKeyCookie);
//            }
//            httpContext.Session[SPContextKey] = spAcsContext;
//        }
//    }
//    #endregion ACS
//    #region HighTrust
//    /// <summary>
//    /// Encapsulates all the information from SharePoint in HighTrust mode.
//    /// </summary>
//    public class SharePointHighTrustContext : SharePointContext
//    {
//        private readonly WindowsIdentity logonUserIdentity;
//        /// <summary>
//        /// The Windows identity for the current user.
//        /// </summary>
//        public WindowsIdentity LogonUserIdentity
//        {
//            get { return this.logonUserIdentity; }
//        }
//        public override string UserAccessTokenForSPHost
//        {
//            get
//            {
//                return GetAccessTokenString(ref this.userAccessTokenForSPHost,
//                                            () => TokenHelper.GetS2SAccessTokenWithWindowsIdentity(this.SPHostUrl, this.LogonUserIdentity));
//            }
//        }
//        public override string UserAccessTokenForSPAppWeb
//        {
//            get
//            {
//                if (this.SPAppWebUrl == null)
//                {
//                    return null;
//                }
//                return GetAccessTokenString(ref this.userAccessTokenForSPAppWeb,
//                                            () => TokenHelper.GetS2SAccessTokenWithWindowsIdentity(this.SPAppWebUrl, this.LogonUserIdentity));
//            }
//        }
//        public override string AppOnlyAccessTokenForSPHost
//        {
//            get
//            {
//                return GetAccessTokenString(ref this.appOnlyAccessTokenForSPHost,
//                                            () => TokenHelper.GetS2SAccessTokenWithWindowsIdentity(this.SPHostUrl, null));
//            }
//        }
//        public override string AppOnlyAccessTokenForSPAppWeb
//        {
//            get
//            {
//                if (this.SPAppWebUrl == null)
//                {
//                    return null;
//                }
//                return GetAccessTokenString(ref this.appOnlyAccessTokenForSPAppWeb,
//                                            () => TokenHelper.GetS2SAccessTokenWithWindowsIdentity(this.SPAppWebUrl, null));
//            }
//        }
//        public SharePointHighTrustContext(Uri spHostUrl, Uri spAppWebUrl, string spLanguage, string spClientTag, string spProductNumber, WindowsIdentity logonUserIdentity)
//            : base(spHostUrl, spAppWebUrl, spLanguage, spClientTag, spProductNumber)
//        {
//            if (logonUserIdentity == null)
//            {
//                throw new ArgumentNullException("logonUserIdentity");
//            }
//            this.logonUserIdentity = logonUserIdentity;
//        }
//        /// <summary>
//        /// Ensures the access token is valid and returns it.
//        /// </summary>
//        /// <param name="accessToken">The access token to verify.</param>
//        /// <param name="tokenRenewalHandler">The token renewal handler.</param>
//        /// <returns>The access token string.</returns>
//        private static string GetAccessTokenString(ref Tuple<string, DateTime> accessToken, Func<string> tokenRenewalHandler)
//        {
//            RenewAccessTokenIfNeeded(ref accessToken, tokenRenewalHandler);
//            return IsAccessTokenValid(accessToken) ? accessToken.Item1 : null;
//        }
//        /// <summary>
//        /// Renews the access token if it is not valid.
//        /// </summary>
//        /// <param name="accessToken">The access token to renew.</param>
//        /// <param name="tokenRenewalHandler">The token renewal handler.</param>
//        private static void RenewAccessTokenIfNeeded(ref Tuple<string, DateTime> accessToken, Func<string> tokenRenewalHandler)
//        {
//            if (IsAccessTokenValid(accessToken))
//            {
//                return;
//            }
//            DateTime expiresOn = DateTime.UtcNow.Add(TokenHelper.HighTrustAccessTokenLifetime);
//            if (TokenHelper.HighTrustAccessTokenLifetime > AccessTokenLifetimeTolerance)
//            {
//                // Make the access token get renewed a bit earlier than the time when it expires
//                // so that the calls to SharePoint with it will have enough time to complete successfully.
//                expiresOn -= AccessTokenLifetimeTolerance;
//            }
//            accessToken = Tuple.Create(tokenRenewalHandler(), expiresOn);
//        }
//    }
//    /// <summary>
//    /// Default provider for SharePointHighTrustContext.
//    /// </summary>
//    public class SharePointHighTrustContextProvider : SharePointContextProvider
//    {
//        private const string SPContextKey = "SPContext";
//        protected override SharePointContext CreateSharePointContext(Uri spHostUrl, Uri spAppWebUrl, string spLanguage, string spClientTag, string spProductNumber, HttpRequestBase httpRequest)
//        {
//            WindowsIdentity logonUserIdentity = httpRequest.LogonUserIdentity;
//            if (logonUserIdentity == null || !logonUserIdentity.IsAuthenticated || logonUserIdentity.IsGuest || logonUserIdentity.User == null)
//            {
//                return null;
//            }
//            return new SharePointHighTrustContext(spHostUrl, spAppWebUrl, spLanguage, spClientTag, spProductNumber, logonUserIdentity);
//        }
//        protected override bool ValidateSharePointContext(SharePointContext spContext, HttpContextBase httpContext)
//        {
//            SharePointHighTrustContext spHighTrustContext = spContext as SharePointHighTrustContext;
//            if (spHighTrustContext != null)
//            {
//                Uri spHostUrl = SharePointContext.GetSPHostUrl(httpContext.Request);
//                WindowsIdentity logonUserIdentity = httpContext.Request.LogonUserIdentity;
//                return spHostUrl == spHighTrustContext.SPHostUrl &&
//                       logonUserIdentity != null &&
//                       logonUserIdentity.IsAuthenticated &&
//                       !logonUserIdentity.IsGuest &&
//                       logonUserIdentity.User == spHighTrustContext.LogonUserIdentity.User;
//            }
//            return false;
//        }
//        protected override SharePointContext LoadSharePointContext(HttpContextBase httpContext)
//        {
//            return httpContext.Session[SPContextKey] as SharePointHighTrustContext;
//        }
//        protected override void SaveSharePointContext(SharePointContext spContext, HttpContextBase httpContext)
//        {
//            httpContext.Session[SPContextKey] = spContext as SharePointHighTrustContext;
//        }
//    }
//    #endregion HighTrust
//}