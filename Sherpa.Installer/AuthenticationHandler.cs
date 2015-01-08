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
            ICredentials credentials = null;
            if (isSharePointOnline)
            {
                credentials = GetCredentialsForSharePointOnline(userName, urlToSite);
                Log.Info("Authenticating with specified SPO credentials");
            }
            else
            {
                credentials = GetCredentialsForSharePointOnPrem(userName, urlToSite);
                if (credentials != null)
                {
                    Log.Info("Authenticating with specified credentials");
                }
                else
                {
                    credentials = CredentialCache.DefaultCredentials;
                    Log.Info("Authenticating with default credentials");
                }
            }
            return credentials;
        }

        public SharePointOnlineCredentials GetCredentialsForSharePointOnline(string userName, Uri urlToSite)
        {
            var password = GetPasswordFromWindowsCredentialManager(userName, urlToSite);
            while (true)
            {
                if (password.Length == 0) password = PromptForPassword(userName);
                if (password != null && password.Length > 0)
                {
                    var credentials = new SharePointOnlineCredentials(userName, password);
                    if (AuthenticateUser(credentials, urlToSite))
                    {
                        Log.Debug("Account successfully authenticated with SPO");
                        return credentials;
                    }
                }
                else
                {
                    return GetCredentialsForSharePointOnline(userName, urlToSite);
                }
            }
        }

        public ICredentials GetCredentialsForSharePointOnPrem(string userName, Uri urlToSite)
        {
            var password = GetPasswordFromWindowsCredentialManager(userName, urlToSite);
            //If there is no password specified in the credential manager and no username is set, we want to use default credentials
            if (password.Length == 0 && string.IsNullOrEmpty(userName))
            {
                return null;
            }
            while (true)
            {
                if (password.Length == 0) 
                    password = PromptForPassword(userName);
                if (password != null && password.Length > 0)
                {
                    Log.Debug("Attempting to authenticate with NetworkCredentials");
                    return new NetworkCredential(userName, password);
                }
                else
                {
                    return GetCredentialsForSharePointOnPrem(userName, urlToSite);
                }
            }
        }

        private SecureString GetPasswordFromWindowsCredentialManager(string userName, Uri urlToSite)
        {
            var password = new SecureString();
            var credentialsFromWindowsCredentialManager = GetCredentialsFromWindowsCredentialManager(urlToSite);
            if (credentialsFromWindowsCredentialManager != null)
            {
                Log.Debug("Trying to authenticate with Windows Credentials Manager");
                userName = credentialsFromWindowsCredentialManager.UserName;
                foreach (char c in credentialsFromWindowsCredentialManager.Password)
                {
                    password.AppendChar(c);
                }
            }
            return password;
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
