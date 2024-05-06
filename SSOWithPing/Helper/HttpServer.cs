using System;
using System.Collections.Generic;
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
                Console.WriteLine("Server started. Listening on " + listeningUrl);
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
            ProcessRequest(context);
            ListenForConnections(); // Continue listening for new connections
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            // Extract the authorization code from the query string
            string query = request.Url.Query;
            var queryParams = System.Web.HttpUtility.ParseQueryString(query);
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

        public void Stop()
        {
            listener.Stop();
            Console.WriteLine("Server stopped.");
        }
    }
}
