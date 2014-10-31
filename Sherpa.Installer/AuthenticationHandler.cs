using System;
using System.Reflection;
using System.Security;
using log4net;
using Microsoft.SharePoint.Client;

namespace Sherpa.Installer
{
    public class AuthenticationHandler
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public SharePointOnlineCredentials GetCredentialsForSharePointOnline(string userName, Uri urlToSite)
        {
            var password = new SecureString();
            var credentialsFromWindowsCredentialManager = GetCredentialsFromWindowsCredentialManager(urlToSite);
            if (credentialsFromWindowsCredentialManager != null)
            {
                Log.Info("Trying to authenticate with Windows Credentials Manager");
                userName = credentialsFromWindowsCredentialManager.UserName;
                foreach (char c in credentialsFromWindowsCredentialManager.Password)
                {
                    password.AppendChar(c);
                }
            }
            while (true)
            {
                if (password.Length == 0) password = PromptForPassword(userName);
                if (password != null && password.Length > 0)
                {
                    var credentials = new SharePointOnlineCredentials(userName, password);
                    if (AuthenticateUser(credentials, urlToSite))
                    {
                        Log.Info("Account successfully authenticated with SPO");
                        return credentials;
                    }
                    Log.Error("Couldn't authenticate user. Try again.");
                }
                else
                {
                    return GetCredentialsForSharePointOnline(userName, urlToSite);
                }
            }
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
