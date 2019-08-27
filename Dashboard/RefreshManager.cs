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

using OfxClient.Data;
using OfxClient.IO;
using OfxClient.Serializers;

namespace Dashboard
{
    class RefreshManager
    {

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

            string resp = "<OFX><SIGNONMSGSRSV1><SONRS><STATUS><CODE>15500<SEVERITY>ERROR<MESSAGE>Login failed due to invalid credentials.</STATUS><DTSERVER>20190827080528[-5:EST]<LANGUAGE>ENG<DTPROFUP>20140605083000<FI><ORG>Vanguard<FID>15103</FI><SESSCOOKIE>2934F61E88A2F352F49E64790C5E582B.JUH7BwK7O47sD_hnwprd01_1</SONRS></SIGNONMSGSRSV1><SIGNUPMSGSRSV1><ACCTINFOTRNRS><TRNUID>6A054DCB-AABA-4EE3-B63D-133EB64F6B11<STATUS><CODE>15500<SEVERITY>ERROR<MESSAGE>Invalid signon</STATUS></ACCTINFOTRNRS></SIGNUPMSGSRSV1></OFX>";
            (new ResponseParser()).Parse(resp);


            var error = VerifyFinancialInstitution("15103");
            if (error != null)
            {
                Console.WriteLine(error);
            }
        }

        private string VerifyFinancialInstitution(string idOfInstitutionToCheck)
        {
            // Create request
            var request = new FinancialInstitutionInformationRequest("Intuit", idOfInstitutionToCheck);

            // Transact it
            var transactionManager = new TransactionManager();
            var doc = transactionManager.Transact(request, null);

            if (doc.Error != null)
            {
                return doc.Error;
            }

            // Check sign-on status
            var error = RequestBuilder.GetSignonStatus(doc);
            if (error != "0")
            {
                return "Signon error " + error;
            }

            string[] detailsLocation =
                { "INTU.BRANDMSGSRSV1", "INTU.BRANDTRNRS", "INTU.BRANDRS", "INTU.BRANDDATALIST", "INTU.BRANDDATA", "INTU.BRANDDETAILS",  };
            var details = doc.Sgml.FindAggregate(detailsLocation);

            var org = details.FindValue(new string[] { "FI", "ORG" });
            var fid = details.FindValue(new string[] { "FI", "FID" });
            var url = details.FindValue(new string[] { "INTU.FIPROFILEURL" });

            if (!FinancialInstitution.FinancialInstitutions[idOfInstitutionToCheck].IsSameInstitution(org, fid, url))
            {
                return $"Institution {org} - id {idOfInstitutionToCheck} - does not match";
            }

            return null;
        }

        public void Connect()
        {
            //Connect("Test", "USERNAME", "PASSWORD", null);
            //Connect("Reference", "GnuCash", "gcash", null);
            Connect("15103", "NotreFric", "4G0r1lla5", null);
            //Connect("Fidelity", "5847654", "def", null);
            //Connect("AFS", "025781392", "C0l0mb13n", null);
            //Connect("Chase", "", "", null);
        }

        private string Connect(string fi, string user, string password, string account)
        {
            var cookies = new List<IEnumerable<string>>();

            // Create transaction manager
            var transactionManager = new TransactionManager();

            // Create profile transaction request
            OfxRequest request = new ProfileRequest("15103");

            // Transact profile request
            var profileDoc = transactionManager.Transact(request, cookies);

            if (profileDoc.Error != null)
            {
                return "Erro in getting profile: " + profileDoc.Error;
            }

            // Check sign-on status
            var error = RequestBuilder.GetSignonStatus(profileDoc);
            if (error != "0")
            {
                return "Signon error " + error;
            }

            // Check profile status
            error = RequestBuilder.GetProfileStatus(profileDoc);
            if (error != "0")
            {
                return "Profile error " + error;
            }

            // Retreive info from profile request (including URL to use!)
            string url = RequestBuilder.GetCoreUrl(profileDoc);

            // Then issue an accounts request
            request = new AccountsRequest(fi, url, user, password);
            var accountsDoc = transactionManager.Transact(request, cookies);

            // Success
            return accountsDoc.Error;
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
