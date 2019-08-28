using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

using OfxClient.Data;
using OfxClient.Serializers;

namespace OfxClient.IO
{
    public class TransactionManager
    {
        private static HttpClient httpClient;

        static TransactionManager()
        {
            // Use Quicken client certificate
            var handler = new HttpClientHandler();
            var x = System.IO.File.ReadAllText("C:\\Program Files (x86)\\Quicken\\certs\\f73e89fd.0");
            var certificate = new X509Certificate2("C:\\Program Files (x86)\\Quicken\\certs\\f73e89fd.0");
            handler.ClientCertificates.Add(certificate);
            handler.ClientCertificateOptions = ClientCertificateOption.Automatic;
            
            httpClient = new HttpClient(handler);

            // Only accept x-ofx
            httpClient.DefaultRequestHeaders.Accept.Clear();
            //httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-ofx"));
            //httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain")); // AFS returns plain text

            // Headers
            //httpClient.DefaultRequestHeaders.Add("User-Agent", "Dashboard 1.0");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "QWIN");

        }

        public OfxDocument Transact(OfxRequest ofxRequest, List<IEnumerable<string>> cookies)
        {
            string comment = null;
            string request = null;
            string result = null;
            OfxDocument document = null;

            var financialInstitution = ofxRequest.FinancialInstitution;
            var url = financialInstitution.ProfileURL;

            // Create request generator
            var requestBuilder = new RequestBuilder(financialInstitution);

            // Create request
            if (ofxRequest is FinancialInstitutionInformationRequest financialInstitutionInformationRequest)
            {
                comment = "Financial institution information";
                request = requestBuilder.GetFinancialInstitutionInformationMessageSet(financialInstitutionInformationRequest.IdOfInstitutionToCheck);
            }
            else if (ofxRequest is ProfileRequest)
            {
                comment = "Profile";
                request = requestBuilder.GetProfileMessageSet();
            }
            else if (ofxRequest is AccountsRequest accountsRequest)
            {
                comment = "Accounts";
                request = requestBuilder.GetAccountsMessageSet(accountsRequest.User, accountsRequest.Password);
                url = accountsRequest.URL;
            }

            // Setup http client
            //httpClient.BaseAddress = new Uri(url);

            // Setup content
            var content = new StringContent(request, Encoding.ASCII, "application/x-ofx");
            Console.WriteLine($"{comment} request:");
            Console.WriteLine(request);

            // Post
            try
            {
                // Send POST request over, and wait for result
                var httpResult = httpClient.PostAsync(url, content).Result;

                // check HTTP status
                if (!httpResult.IsSuccessStatusCode)
                {
                    string error = $"HTTP error {httpResult.StatusCode}";
                    Console.WriteLine(error);
                    return new OfxDocument(error, null, null);
                }

                // Extract result as string 
                result = httpResult.Content.ReadAsStringAsync().Result;

                Console.WriteLine($"{comment} request result:");
                Console.WriteLine(result);

                // We now have the result as a string - parse it and create a document
                document = ResponseParser.Parse(result);

                // Get the signon status
                var signonStatus = RequestBuilder.GetSignonStatus(document);

                // Find returned cookies
                if (cookies != null)
                {
                    bool foundCookies = false;
                    foreach (var header in httpResult.Headers)
                    {
                        if (header.Key == "Set-Cookie")
                        {
                            cookies.Add(header.Value);
                            foundCookies = true;
                        }
                    }

                    bool resendWithCookies = true; // ZZZZ
                    if (resendWithCookies && signonStatus != "0" && foundCookies)
                    {
                        content = new StringContent(request, Encoding.ASCII, "application/x-ofx");
                        foreach (var cookie in cookies)
                        {
                            content.Headers.Add("Set-Cookie", cookie);
                        }
                        httpResult = httpClient.PostAsync(url, content).Result;
                        result = httpResult.Content.ReadAsStringAsync().Result;
                        document = ResponseParser.Parse(result);
                        Console.WriteLine($"{comment} SECOND request result:");
                        Console.WriteLine(result);
                    }
                }
            }
            catch (Exception ex)
            {
                var error = $"{comment} request exception: {ex.Message}";
                Console.WriteLine(error);
                return new OfxDocument(error, null, null);
            }

            return document;
        }
    }
}
