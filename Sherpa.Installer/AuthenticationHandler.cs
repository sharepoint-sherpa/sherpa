using System;
using System.Security;
using Microsoft.SharePoint.Client;

namespace Sherpa.Installer
{
    public class AuthenticationHandler
    {
        public SharePointOnlineCredentials GetCredentialsForSharePointOnline(string userName, Uri urlToSite)
        {
            var password = new SecureString();
            var credentialsFromWindowsCredentialManager = GetCredentialsFromWindowsCredentialManager(urlToSite);
            if (credentialsFromWindowsCredentialManager != null)
            {
                Console.WriteLine("Trying to authenticate with Windows Credentials Manager");
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
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Account successfully authenticated!");
                        Console.ResetColor();

                        return credentials;
                    }
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Couldn't authenticate user. Try again.");
                    Console.ResetColor();
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
