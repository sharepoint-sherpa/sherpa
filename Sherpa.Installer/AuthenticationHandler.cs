using System;
using System.Net;
using System.Reflection;
using System.Security;
using log4net;
using Microsoft.SharePoint.Client;

namespace Sherpa.Installer
{
    public class AuthenticationHandler
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public ICredentials GetSessionAuthCredentials(bool isSharePointOnline, string userName, Uri urlToSite)
        {
            ICredentials credentials = GetCredentialsForSharePoint(userName, urlToSite, isSharePointOnline);
            if (credentials != null)
            {
                Log.Info("Authenticating with specified credentials");
            }
            else if (!isSharePointOnline)
            {
                credentials = CredentialCache.DefaultCredentials;
                Log.Info("Authenticating with default credentials (on-prem)");
            }
            
            return credentials;
        }

        public ICredentials GetCredentialsForSharePoint(string userName, Uri urlToSite, bool isSharePointOnline)
        {
            var credentials = GetCredentialsFromWindowsCredentialManager(urlToSite, isSharePointOnline);
            //If there is no creds specified in the credential manager and no username is set, we want to use default credentials
            if (credentials == null && !string.IsNullOrEmpty(userName))
            {
                var password = PromptForPassword(userName);
                if (password != null && password.Length > 0)
                {
                    if (isSharePointOnline)
                    {
                        Log.Debug("Attempting to authenticate with SharePointOnlineCredentials");
                        credentials = new SharePointOnlineCredentials(userName, password);
                        if (!AuthenticateUser((SharePointOnlineCredentials) credentials, urlToSite))
                        {
                            Log.Error("There is a problem authenticating with the provided credentials");
                            GetCredentialsForSharePoint(userName, urlToSite, false);
                        }
                    }
                    else
                    {
                        try
                        {
                            Log.Debug("Attempting to authenticate with NetworkCredentials");
                            credentials = new NetworkCredential(userName, password);
                        }
                        catch
                        {
                            Log.Error("There is a problem with the provided credentials");
                            GetCredentialsForSharePoint(userName, urlToSite, false);
                        }
                    }
                }
            }
            
            return credentials;
        }

        private ICredentials GetCredentialsFromWindowsCredentialManager(Uri urlToSite, bool isSharePointOnline)
        {
            var credentialsFromWindowsCredentialManager = GetCredentialsFromWindowsCredentialManager(urlToSite);
            if (credentialsFromWindowsCredentialManager != null)
            {
                Log.Debug("Trying to authenticate with credentials from Windows Credentials Manager");
                var userName = credentialsFromWindowsCredentialManager.UserName;
                var password = new SecureString();
                foreach (char c in credentialsFromWindowsCredentialManager.Password)
                {
                    password.AppendChar(c);
                }
                if (isSharePointOnline)
                {
                    Log.Debug("Retrieved SPO credentials for user " + userName);
                    return new SharePointOnlineCredentials(userName, password);
                }
                Log.Debug("Retrieved on-premises credentials for user " + userName);
                return new NetworkCredential(userName, password);
            }
            return null;
        }

        private static SecureString PromptForPassword(string userName)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Enter your password for {0}: ", userName);
            var password = PasswordReader.GetConsoleSecurePassword();
            Console.ResetColor();
            Console.WriteLine();
            return password;
        }

        private Credential GetCredentialsFromWindowsCredentialManager(Uri urlToSite)
        {
            return CredentialManager.ReadCredential(urlToSite.Host);
        }

        /// <summary>
        /// TODO: How to check authentication in on-prem scenarios?
        /// </summary>
        /// <param name="credentials"></param>
        /// <param name="urlToSite"></param>
        /// <returns></returns>
        private bool AuthenticateUser(SharePointOnlineCredentials credentials, Uri urlToSite)
        {
            try
            {
                credentials.GetAuthenticationCookie(urlToSite);
                return true;
            }
            catch (IdcrlException)
            {
                return false;
            }
        }
    }
}
