using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Rascal
{
    class Listener
    {
        private HttpListener listener = new HttpListener();

        public void Run()
        {
            // Setup the prefixes

            // For a simple (non-secure) my.benoit.com server to work:
            // - Disable service "World Wide Web Publishing Service" (it keeps port 80 open)
            // - Reserve the right with "netsh http add urlacl url=http://my.benoit.com:80/ user=Everyone"
            // - Add a line "127.0.0.1 my.benoit.com" to C;\Windows\System32\Drivers\etc\hosts"
            listener.Prefixes.Add("http://my.benoit.com/");

            // Those would need server certificates.
            //listener.Prefixes.Add("https://ofx-prod-brand.intuit.com/qw2800/fib.dll/");
            //listener.Prefixes.Add("https://vesnc.vanguard.com/us/OfxProfileServlet/");
            //listener.Prefixes.Add("https://vesnc.vanguard.com/us/OfxDirectConnectServlet/");

            // Start listening
            listener.Start();
            Console.WriteLine("Listening...");

            while (true)
            {
                // Wait for a request 
                HttpListenerContext context = listener.GetContext();

                // Get the request
                HttpListenerRequest request = context.Request;

                // Dump the request
                // ZZZ request.

                // Obtain a response object.
                HttpListenerResponse response = context.Response;

                // Build a response.
                string responseString = "<HTML><BODY> Made it!</BODY></HTML>";
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);

                // Get a response stream and write the response to it.
                response.ContentLength64 = buffer.Length;
                System.IO.Stream output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);

                // Always close the output stream.
                output.Close();

                // Send the response
            }
        }
    }
}
