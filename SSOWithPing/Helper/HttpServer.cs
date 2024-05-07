using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;  // Add reference to System.Web for HttpUtility if needed

namespace SSOWithPing.Helper
{
    public class HttpServer
    {
        private HttpListener listener;
        private string listeningUrl;
        public delegate Task AuthorizationCodeReceivedHandler(string code);
        public event AuthorizationCodeReceivedHandler OnAuthorizationCodeReceived;

       // public Action<string> OnAuthorizationCodeReceived; // Delegate to handle the authorization code

        public HttpServer(string url)
        {
            listeningUrl = url;
            listener = new HttpListener();
            listener.Prefixes.Add(listeningUrl);
        }

        public void Start()
        {
            try
            {
                listener.Start();
                Debug.WriteLine("Server started. Listening on " + listeningUrl);
                ListenForConnections();
            }
            catch (HttpListenerException ex)
            {
                Console.WriteLine("Failed to start server: " + ex.Message);
            }
        }

        private void ListenForConnections()
        {
            listener.BeginGetContext(new AsyncCallback(HandleRequest), listener);
        }

        private void HandleRequest(IAsyncResult result)
        {
            var context = listener.EndGetContext(result);
            string path = context.Request.Url.AbsolutePath;
            if (String.Equals(path, "/logout", StringComparison.OrdinalIgnoreCase))
            {
                HandleLogoutRequest(context);
            }
            else
            {
                // Handle other requests
                ProcessRequest(context);
            }
            ListenForConnections(); // Continue listening for new connections
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            // Extract the authorization code from the query string
            string query = request.Url.Query;
            var queryParams = HttpUtility.ParseQueryString(query);
            string code = queryParams["code"];

            if (!string.IsNullOrEmpty(code) && OnAuthorizationCodeReceived != null)
            {
                // If the code is available and the delegate is set, invoke it
                OnAuthorizationCodeReceived.Invoke(code);
            }

            // Prepare and send the response to the user
            string responseString = "<html><head><title>Authentication Successful</title></head><body>Please return to the app.</body></html>";
            byte[] buffer = Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }

        // Hypothetical server-side handler for logout
        public void HandleLogoutRequest(HttpListenerContext context)
        {
            // Clear session cookies or any other session data
            //ClearSession(context);

            // Send a response to indicate successful logout or redirect to a login page
            var response = context.Response;
            var buffer = System.Text.Encoding.UTF8.GetBytes("You have been logged out. <a href='login.html'>Login again</a>");
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }

        public void Stop()
        {
            listener.Stop();
            Console.WriteLine("Server stopped.");
        }
    }
}
