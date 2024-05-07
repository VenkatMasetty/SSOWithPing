using IdentityModel.OidcClient.Browser;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SSOWithPing.Helper
{
    public class SystemBrowser : IBrowser
    {
        private readonly int _port;
        private readonly string _path = "/callback";
        private HttpListener _httpListener;
        public SystemBrowser(int port)
        {
            _port = port;
        }

        public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = default)
        {
            using (_httpListener = new HttpListener())
            {
                _httpListener.Prefixes.Add($"http://localhost:{_port}{_path}/");
                _httpListener.Start();
                try
                {
                    // Open the URL in the default system browser
                    OpenUrl(options.StartUrl);
                    // Wait for the OAuth callback
                    var context = await _httpListener.GetContextAsync();
                    var formData = context.Request.QueryString;
                    // Close the HTTP listene
                    _httpListener.Close();
                    // Retrieve authorization response
                    var result = new BrowserResult
                    {
                        Response = formData.ToString(),
                        ResultType = BrowserResultType.Success
                    };
                    // Send OK response
                    var response = context.Response;
                    var buffer = System.Text.Encoding.UTF8.GetBytes("<html><head><meta http-equiv='refresh' content='10;url=https://example.com'></head><body>Please return to the app.</body></html>");
                    response.ContentLength64 = buffer.Length;
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                    response.OutputStream.Close();
                    return result;
                }
                catch (Exception ex)
                {
                    return new BrowserResult
                    {
                        ResultType = BrowserResultType.UnknownError,
                        Error = ex.ToString()
                    };
                }
            }
        }
        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                // Log or handle exceptions if the browser fails to open
                Debug.WriteLine("Failed to open URL in the default browser: " + ex.Message);
            }

        }

    }
}
