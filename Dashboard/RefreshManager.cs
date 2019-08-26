using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard
{
    class RefreshManager
    {
        public enum EAction { Profile, Accounts, Investments, FinancialInstitutionInformation };

        public RefreshManager()
        {
            // To find out which TLS version the http client is running by default:
            // 8/25/2019: It's TLS 1.2
            //var str = new HttpClient().GetStringAsync("https://www.howsmyssl.com").Result;
            // Console.WriteLine(str);
        }

        public void GetFinancialInstitutionInfo()
        {
            /*
            // So where to find info about financial institutions?
            //
            // gnucash uses libofx, which fetches a file with presumably a list of financial institution like so:
            // post("T=1&S=*&R=1&O=0&TEST=0", "http://moneycentral.msn.com/money/2005/mnynet/service/ols/filist.aspx?SKU=3&VER=6", kBankFilename);
            // post("T=3&S=*&R=1&O=0&TEST=0", "http://moneycentral.msn.com/money/2005/mnynet/service/ols/filist.aspx?SKU=3&VER=6", kInvFilename);
            //
            // I tried it this way and that, it loks like the file is gone from msn.com. I tried then looking in quicken support, and if you dig
            // carefully you get to an intuit page at https://fi.intuit.com/fisearchbasic/personal/quicken/basic_search/Quicken_windows.html
            // that lists what financial institutions support. It turns out the site fi.intuit.com is trying to convince financial institutions
            // to use ofx, and provides tools/support to integrate with quicken.
            //
            // Finally, by googling "filist" I found one in json at:
            // https://github.com/arolson101/filist/blob/master/filist.json
            // I also found one at https://www.npmjs.com/package/filist that I did not dare open and one at https://ofx-prod-filist.intuit.com/qb2600/data/fidir.txt
            // that does not have investment firms.
            //

            // Create HTTP client
            var httpClient = new HttpClient();

            // Setup URL
            var url = new Uri("http://moneycentral.msn.com/money/2005/mnynet/service/ols/filist.aspx?SKU=3&VER=6&T=1&S=*&R=1&O=0&TEST=0");
            url = new Uri("http://moneycentral.msn.com/money/2005/mnynet/service/olsvcupd/OnlSvcBrandInfo.aspx?MSNGUID=&GUID=15103&SKU=3&VER=6");
            //url = new Uri("http://127.0.0.1/go-list");
            //httpClient.BaseAddress = new Uri(financialInstitution.URL);

            // Only accept x-ofx
            //httpClient.DefaultRequestHeaders.Accept.Clear();
            //httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-ofx"));
            //httpClient.DefaultRequestHeaders.Add("User-Agent", "Dashboard 1.0");

            // Setup content
            //var content = new StringContent("T=1&S=*&R=1&O=0&TEST=0", Encoding.ASCII, "text/plain");

            // Post
            try
            {
                var result = httpClient.GetAsync(url).Result;
                //var result = httpClient.PostAsync(url, content).Result;
                string resultString = result.Content.ReadAsStringAsync().Result;
                Console.WriteLine("Request result:");
                Console.WriteLine(resultString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Request exception: {ex.Message}");
            }

            httpClient.Dispose();
            */

            VerifyFinancialInstitution("15103");
        }

        private void VerifyFinancialInstitution(string idOfInstitutionToCheck)
        {
            // Get intuit server info
            var financialInstitution = FinancialInstitution.FinancialInstitutions["Intuit"];

            // Create request generator
            var requestBuilder = new OfxRequestBuilder(financialInstitution);

            // Create HTTP client
            var httpClient = SetupHttpClient(financialInstitution);

            // Send request
            SendMessage(httpClient, requestBuilder, null, EAction.FinancialInstitutionInformation, financialInstitution.ProfileURL, null, null, idOfInstitutionToCheck);

            // ZZZ Get result and verify status OK
            // Parse answer
            var parser = new OfxResponseParser();
            string str = System.IO.File.ReadAllText("response.txt");
            var doc = parser.Parse(str);

            string[] detailsLocation =
                { "INTU.BRANDMSGSRSV1", "INTU.BRANDTRNRS", "INTU.BRANDRS", "INTU.BRANDDATALIST", "INTU.BRANDDATA", "INTU.BRANDDETAILS",  };
            var details = doc.Sgml.FindAggregate(detailsLocation);

            var org = details.FindValue(new string[] { "FI", "ORG" });
            var fid = details.FindValue(new string[] { "FI", "FID" });
            var url = details.FindValue(new string[] { "INTU.FIPROFILEURL" });

            if (!FinancialInstitution.FinancialInstitutions[idOfInstitutionToCheck].IsSameInstitution(org, fid, url))
            {
                throw new InvalidOperationException($"Institution {org} - id {idOfInstitutionToCheck} - does not match");
            }
        }

        public void Connect()
        {
            //Connect("Test", "USERNAME", "PASSWORD", EAction.Accounts, null);
            //Connect("Reference", "GnuCash", "gcash", EAction.Accounts, null);
            Connect("15103", "NotreFric", "XXXXXX", EAction.Accounts, null);
            //Connect("Fidelity", "5847654", "def", EAction.Profile, null);
            //Connect("AFS", "025781392", "C0l0mb13n", EAction.Accounts, null);
            //Connect("Chase", "", "", EAction.Accounts, null);
        }

        private void Connect(string fi, string user, string password, EAction action, string account)
        {
            var cookies = new List<IEnumerable<string>>();

            // Get financial institution
            var financialInstitution = FinancialInstitution.FinancialInstitutions[fi];

            // Create request generator
            var requestBuilder = new OfxRequestBuilder(financialInstitution);

            // Create HTTP client
            var httpClient = SetupHttpClient(financialInstitution);

            // Deal with challenge if password is encrypted
            if (financialInstitution.EncryptedPassword)
            {
                SendChallengeRequest(httpClient, requestBuilder, user);
            }

            // First issue a profile request
            SendMessage(httpClient, requestBuilder, cookies, EAction.Profile, financialInstitution.ProfileURL, null, null, null);

            // Retreive info from profile request (including URL to use!)
            // ZZZZ

            // Then issue what the user wants
            // SendMessage(httpClient, requestBuilder, cookies, action, ZZZURL, user, password, account);



            httpClient.Dispose();
        }

        private HttpClient SetupHttpClient(FinancialInstitution financialInstitution)
        {
            // Create HTTP client
            var httpClient = new HttpClient();

            //specify to use TLS 1.2 as default connection
            // Not necessary after .Net 4.6.2
            // System.Net.ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            // Setup URL
            httpClient.BaseAddress = new Uri(financialInstitution.ProfileURL);

            // Only accept x-ofx
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-ofx"));
            //httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain")); // AFS returns plain text
            //httpClient.DefaultRequestHeaders.Add("User-Agent", "Dashboard 1.0");
            //httpClient.DefaultRequestHeaders.Add("User-Agent", "QWIN");
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Dashboard 1.0");

            return httpClient;
        }

        private void SendChallengeRequest(HttpClient httpClient, OfxRequestBuilder requestBuilder, string user)
        {
            var challengeRequest = requestBuilder.GetChallengeMessageSet(user);
            var challengeContent = new StringContent(challengeRequest, Encoding.ASCII, "application/x-ofx");
            try
            {
                var result = httpClient.PostAsync(requestBuilder.FinancialInstitution.ChallengeURL, challengeContent).Result;
                string resultString = result.Content.ReadAsStringAsync().Result;
                Console.WriteLine("Challenge request result:");
                Console.WriteLine(resultString);
                return; // RFU, haven't found a server yet that wants TYPE1 security.
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Challenge request exception: {ex.Message}");
                return;
            }
        }

        private void SendMessage(HttpClient httpClient, OfxRequestBuilder requestBuilder, List<IEnumerable<string>> cookies, EAction action, string url, string user, string password, string account)
        {
            string comment = null;
            string request = null;

            switch (action)
            {
                case EAction.Accounts:
                    comment = "Accounts";
                    request = requestBuilder.GetAccountsMessageSet(user, password);
                    break;

                case EAction.Investments:
                    comment = "Investements";
                    request = requestBuilder.GetInvestmentsMessageSet(user, password, account);
                    break;

                case EAction.Profile:
                    comment = "Profile";
                    request = requestBuilder.GetProfileMessageSet();
                    break;

                case EAction.FinancialInstitutionInformation:
                    comment = "Financial institution information";
                    // account is financial institution Id
                    request = requestBuilder.GetFinancialInstitutionInformationMessageSet(account);
                    break;
            }


            // Setup content
            var content = new StringContent(request, Encoding.Default, "application/x-ofx");
            Console.WriteLine($"{comment} request:");
            Console.WriteLine(request);

            // Post
            try
            {
                var result = httpClient.PostAsync(url, content).Result;
                string resultString = result.Content.ReadAsStringAsync().Result;
                Console.WriteLine($"{comment} request result:");
                Console.WriteLine(resultString);

                // Find returned cookies
                bool foundCookies = false;
                bool resendWithCookies = false; // ZZZZ
                foreach (var header in result.Headers)
                {
                    if (header.Key == "Set-Cookie")
                    {
                        cookies.Add(header.Value);
                        foundCookies = true;
                    }
                }

                if (foundCookies && resendWithCookies)
                {
                    foreach(var cookie in cookies)
                    {
                        content.Headers.Add("Set-Cookie", cookie);
                    }
                    content = new StringContent(request, Encoding.ASCII, "application/x-ofx");
                    result = httpClient.PostAsync(url, content).Result;
                    resultString = result.Content.ReadAsStringAsync().Result;
                    Console.WriteLine($"{comment} SECOND request result:");
                    Console.WriteLine(resultString);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{comment} request exception: {ex.Message}");
            }
        }
    }


    //
    // Local listener for debug
    //
    public class Listener
    {
        private TcpListener listener;

        public Listener()
        {
            // Create listener
            listener = new TcpListener(IPAddress.Any, 80);

            // Start listening for client requests.
            listener.Start();

            // Async
            listener.BeginAcceptTcpClient(ProcessConnection, listener);
        }

        private void ProcessConnection(IAsyncResult ar)
        {
            var listener = (TcpListener)ar.AsyncState;

            TcpClient client = listener.EndAcceptTcpClient(ar);

            int sz = client.Available;
            if (sz > 0)
            {
                var buf = new byte[sz];
                client.GetStream().Read(buf, 0, sz);

                string stringTransferred = Encoding.ASCII.GetString(buf, 0, sz);

                Console.WriteLine($"Header:\n{stringTransferred}");
                Console.WriteLine($"Size of header is {sz}");

                string search = "\nContent-Length: ";
                int lenIx = stringTransferred.IndexOf(search);
                if (lenIx >= 0)
                {
                    string contentLength = stringTransferred.Substring(lenIx + search.Length).Split('\n')[0];
                    int contentSize = int.Parse(contentLength);

                    var contentBuf = new byte[contentSize];

                    client.GetStream().Read(contentBuf, 0, contentSize);

                    string content = Encoding.ASCII.GetString(contentBuf, 0, contentSize);

                    Console.WriteLine($"Content:\n{content}");
                    Console.WriteLine($"Size of content is {contentSize}");
                }
            }

            client.Close();
            client.Dispose();

            listener.BeginAcceptTcpClient(ProcessConnection, listener);
        }
    }
}
