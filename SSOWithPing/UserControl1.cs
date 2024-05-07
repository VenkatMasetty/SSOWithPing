using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SSOWithPing.Helper;
using System.Net.Http;
using Newtonsoft.Json;
using CefSharp.WinForms;
using CefSharp;
using System.Web;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SSOWithPing
{
    public partial class UserControl1 : UserControl
    {
        private HttpServer server;
        private string currentCodeVerifier;
        private Button btnLogout;
        public UserControl1()
        {
            InitializeComponent();
            this.Load += UserControl1_Load;
            InitializeLogoutButton();
            Debug.WriteLine("UserControl1: Constructor called.");
        }
        
        private void InitializeLogoutButton()
        {
            btnLogout.Click += BtnLogout_Click;
            this.Controls.Add(btnLogout);
        }
        private void UserControl1_Load(object sender, EventArgs e)
        {
            SetupServer();
        }

        private void SetupServer()
        {
            if (this.ParentForm != null)
            {
                this.ParentForm.FormClosed += ParentForm_FormClosed;
                server = new HttpServer("http://localhost:64663/");
                server.OnAuthorizationCodeReceived += ProcessAuthentication;
                server.Start();
                Debug.WriteLine("Server started.");
            }
            else
            {
                Debug.WriteLine("ParentForm is null, server setup deferred.");
            }
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            try
            {
                // Generate the code verifier and challenge for PKCE to enhance security.
                currentCodeVerifier = AuthenticationHelper.GenerateCodeVerifier();
                string codeChallenge = AuthenticationHelper.GenerateCodeChallenge(currentCodeVerifier);
                string clientId = "d6acae5c-6a3b-4af0-9d26-06270b933815";
                string redirectUri = "http://localhost:64663/callback";
                string authorizationUrl = AuthenticationHelper.CreateAuthorizationUrl(clientId, redirectUri, codeChallenge);

                // Start the authentication process by opening the authorization URL in the browser.
                    System.Diagnostics.Process.Start(authorizationUrl);
            }
            catch (Exception ex)
            {
                // Handle exceptions that occur during the login attempt, such as a failure to start the browser.
                MessageBox.Show("Failed to start authentication process: " + ex.Message);
            }
        }
        private async Task ProcessAuthentication(string code)
        {
            try
            {
                string tokensJson = await ExchangeCodeForTokens(code, currentCodeVerifier);
                var tokenData = JsonConvert.DeserializeObject<dynamic>(tokensJson);
                string accessToken = tokenData.access_token;
                AuthenticationState.IsAuthenticated = true;
                UpdateUIOnSuccess("Login successful! Welcome.");
            }
            catch (HttpRequestException ex)
            {
                UpdateUIOnError($"Login failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                // Handle other possible exceptions
                UpdateUIOnError($"An error occurred: {ex.Message}");
            }
        }

        private async Task<string> ExchangeCodeForTokens(string authorizationCode, string codeVerifier)
        {
            using (var client = new HttpClient())
            {
                var tokenEndpoint = "https://auth.pingone.com/86b8fad2-8f13-4c8d-93b4-6c9affb63b20/as/token";
                var values = new Dictionary<string, string>
             {
            {"grant_type", "authorization_code"},
            {"code", authorizationCode},
            {"redirect_uri", "http://localhost:64663/callback"},
            {"client_id", "d6acae5c-6a3b-4af0-9d26-06270b933815"},
            {"code_verifier", codeVerifier}  // PKCE support
            };

                var content = new FormUrlEncodedContent(values);
                var response = await client.PostAsync(tokenEndpoint, content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"Failed to exchange code for tokens: {response.StatusCode} - {responseString}");
                    var errorDetails = ParseOAuthError(responseString);
                    throw new HttpRequestException($"Request failed - {errorDetails}");
                }

                return responseString;  // Consider returning a more specific piece of data, like an access token
            }
        }

        private string ParseOAuthError(string jsonResponse)
        {
            try
            {
                var errorObj = JsonConvert.DeserializeObject<dynamic>(jsonResponse);
                return $"{errorObj.error}: {errorObj.error_description}";
            }
            catch
            {
                return "Failed to parse error details from OAuth response.";
            }
        }

        public void UpdateUIOnSuccess(string message)
        {
            // Thread safety check to ensure UI updates happen on the correct thread.
            if (InvokeRequired)
            {
                this.Invoke(new Action<string>(UpdateUIOnSuccess), new object[] { message });
                return;
            }

            // Update the UI to reflect successful login and hide the login button.
            btnLogin.Visible = false;
            //webBrowser.DocumentText = message;
            lblStatus.Text = message;
        }

        // Update the UI to show errors during the login process
        public void UpdateUIOnError(string message)
        {
            // Thread safety check to ensure UI updates happen on the correct thread.
            if (InvokeRequired)
            {
                this.Invoke(new Action<string>(UpdateUIOnError), new object[] { message });
                return;
            }

            // Display error messages directly in the UI to inform the user.
            lblStatus.Text = message;
        }
        private void ParentForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Ensure the server stops listening and cleans up when the form closes.
            if (server != null)
            {
                server.Stop();
            }
        }

        private void BtnLogout_Click(object sender, EventArgs e)
        {
            // Clear authentication data
            string logoutUrl = "http://localhost:64663/logout";
            System.Diagnostics.Process.Start(logoutUrl);  // This will open the logout URL in the user's default browser
            AuthenticationState.IsAuthenticated = false;
            // Update UI to reflect logged-out state
            btnLogin.Visible = true;
            btnLogout.Visible = false;
            lblStatus.Text = "Logged out. Please log in again.";

            // Clear any local settings if stored
           // ClearLocalSettings();
        }
    }
}

