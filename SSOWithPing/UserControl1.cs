﻿using System;
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

namespace SSOWithPing
{
    public partial class UserControl1 : UserControl
    {
        private HttpServer server;
        private string currentCodeVerifier;
        //private ChromiumWebBrowser browser;
        private WebBrowser webBrowser;
        public UserControl1()
        {
            InitializeComponent();
            //InitializeChromium();
            InitializeWebBrowser();
            // Register the Load event to initialize components once the control is fully loaded.
            this.Load += UserControl1_Load;

        }
        // Set up the Chromium Embedded Framework (CEF) browser
        //private void InitializeChromium()
        //{
        //    CefSettings settings = new CefSettings();
        //    // Initialization settings for CEF can be configured here
        //    Cef.Initialize(settings);

        //    // Create the ChromiumWebBrowser component and add it to the form
        //    browser = new ChromiumWebBrowser("about:blank")
        //    {
        //        Dock = DockStyle.Fill
        //    };
        //    this.Controls.Add(browser);

        //    // Subscribe to the FrameLoadEnd event to handle redirects and capture the authorization code
        //    browser.FrameLoadEnd += Browser_FrameLoadEnd;
        //}
        // Set up the Chromium Embedded Framework (CEF) browser
        private void InitializeWebBrowser()
        {
           // CefSettings settings = new CefSettings();
            // Initialization settings for CEF can be configured here
           // Cef.Initialize(settings);

            // Create the WebBrowser component and add it to the form
            webBrowser = new WebBrowser
            {
                Dock = DockStyle.Fill
            };
            this.Controls.Add(webBrowser);

            // Subscribe to the FrameLoadEnd event to handle redirects and capture the authorization code
            webBrowser.DocumentCompleted += WebBrowser_DocumentCompleted;
        }

        // Handles the page load events to capture the OAuth redirect and extract the authorization code
        private void WebBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            if (e.Url.ToString().StartsWith("http://localhost:64663/callback"))
            {
                var uri = new Uri(e.Url.ToString());
                var code = System.Web.HttpUtility.ParseQueryString(uri.Query).Get("code");
                if (!string.IsNullOrEmpty(code))
                {
                    ProcessAuthentication(code).ConfigureAwait(false);
                }
            }
        }

        private void UserControl1_Load(object sender, EventArgs e)
        {
            // Confirm that the UserControl is attached to a form before initializing the server.
            if (this.ParentForm != null)
            {
                // Attach to the FormClosed event to properly clean up the server.
                this.ParentForm.FormClosed += ParentForm_FormClosed;
                // Initialize the server to listen on a specific port.
                server = new HttpServer("http://localhost:64663/");
                server.OnAuthorizationCodeReceived += ProcessAuthentication;
                server.Start();
            }
        }

        private void ParentForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Ensure the server stops listening and cleans up when the form closes.
            if (server != null)
            {
                server.Stop();
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
                //System.Diagnostics.Process.Start(authorizationUrl);
                webBrowser.Navigate(authorizationUrl);
            }
            catch (Exception ex)
            {
                // Handle exceptions that occur during the login attempt, such as a failure to start the browser.
                MessageBox.Show("Failed to start authentication process: " + ex.Message);
            }
        }

        // Update the UI to reflect successful login
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

        private async Task ProcessAuthentication(string code)
        {
            try
            {
                // Call ExchangeCodeForTokens using the code and the stored codeVerifier
                string tokensJson = await ExchangeCodeForTokens(code, currentCodeVerifier);

                // Deserialize JSON response to extract the access token (add more handling as needed)
                var tokenData = JsonConvert.DeserializeObject<dynamic>(tokensJson);
                string accessToken = tokenData.access_token;

                // Update UI on success
                UpdateUIOnSuccess("Login successful! Welcome.");
            }
            catch (Exception ex)
            {
                // Handle errors and update UI accordingly
                UpdateUIOnError("Login failed: " + ex.Message);
            }
        }


        private async Task<string> ExchangeCodeForTokens(string authorizationCode, string codeVerifier)
        {
            using (var client = new HttpClient())
            {
                var tokenEndpoint = "https://auth.pingone.com/86b8fad2-8f13-4c8d-93b4-6c9affb63b20/as/token"; // Change this to your actual token endpoint
                var values = new Dictionary<string, string>
        {
            {"grant_type", "authorization_code"},
            {"code", authorizationCode},
            {"redirect_uri", "http://localhost:64663/callback"},
            {"client_id", "d6acae5c-6a3b-4af0-9d26-06270b933815"},
            {"code_verifier", codeVerifier}  // Assuming you're using PKCE
        };

                var content = new FormUrlEncodedContent(values);
                var response = await client.PostAsync(tokenEndpoint, content);
                var responseString = await response.Content.ReadAsStringAsync();
                return responseString; // For real-world applications, you'd likely want to deserialize this response and handle it accordingly
            }
        }

    }
}

