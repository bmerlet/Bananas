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
        public enum EAction { Profile, Accounts, Investments };

        public RefreshManager()
        {
        }

        public void GetFinancialInstitutionInfo()
        {
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
        }

        public void Connect()
        {
            //Connect("Test", "USERNAME", "PASSWORD", EAction.Accounts, null);
            //Connect("Reference", "GnuCash", "gcash", EAction.Accounts, null);
            Connect("Vanguard2", "", "", EAction.Profile, null);
            //Connect("Fidelity", "5847654", "def", EAction.Profile, null);
            //Connect("AFS", "025781392", "C0l0mb13n", EAction.Accounts, null);
            //Connect("Chase", "", "", EAction.Accounts, null);
        }

        private void Connect(string fi, string user, string password, EAction action, string account)
        {
            // Get financial institution
            var financialInstitution = FinancialInstitution.FinancialInstitutions[fi];

            // Create request generator
            var requestBuilder = new OfxRequestBuilder(financialInstitution);

            // Create HTTP client
            var httpClient = new HttpClient();

            //specify to use TLS 1.2 as default connection
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            // Setup URL
            httpClient.BaseAddress = new Uri(financialInstitution.URL);

            // Only accept x-ofx
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-ofx"));
            //httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain")); // AFS returns plain text
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Dashboard 1.0");

            // When encrypting, get a challenge (random data) from the server
            if (financialInstitution.EncryptedPassword)
            {
                var challengeRequest = requestBuilder.GetChallengeMessageSet(user);
                var challengeContent = new StringContent(challengeRequest, Encoding.ASCII, "application/x-ofx");
                try
                {
                    var result = httpClient.PostAsync(financialInstitution.URL, challengeContent).Result;
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

            string comment = null;
            string request = null;

            switch(action)
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
            }


            // Setup content
            var content = new StringContent(request, Encoding.ASCII, "application/x-ofx");
            Console.WriteLine($"{comment} request:");
            Console.WriteLine(request);

            // Post
            try
            {
                var result = httpClient.PostAsync(financialInstitution.URL, content).Result;
                string resultString = result.Content.ReadAsStringAsync().Result;
                Console.WriteLine($"{comment} request result:");
                Console.WriteLine(resultString);

                // Resend with cookies
                bool foundCookies = false;
                foreach (var header in result.Headers)
                {
                    if (header.Key == "Set-Cookie")
                    {
                        content.Headers.Add(header.Key, header.Value);
                        foundCookies = true;
                    }
                }

                if (foundCookies)
                {
                    content = new StringContent(request, Encoding.ASCII, "application/x-ofx");
                    result = httpClient.PostAsync(financialInstitution.URL, content).Result;
                    resultString = result.Content.ReadAsStringAsync().Result;
                    Console.WriteLine($"{comment} SECOND request result:");
                    Console.WriteLine(resultString);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{comment} request exception: {ex.Message}");
            }


            httpClient.Dispose();
        }

    }

    public class OfxRequestBuilder
    {
        private const string eol = "\r\n";

        private readonly FinancialInstitution financialInstitution;
        private readonly string newFileId;

        private int cookie = 0;

        public OfxRequestBuilder(FinancialInstitution financialInstitution)
        {
            this.financialInstitution = financialInstitution;
            newFileId = GetUID();
        }

        //
        // Header
        //
        public string GetHeader(bool encrypt)
        {
            string header;

            if (financialInstitution.XML)
            {
                header =
                    "<?xml version=\"1.0\">" + eol + eol +
                    "<?OFX OFXHEADER=\"200\" VERSION=\"220\" SECURITY=\"NONE\" OLDFILEUID=\"NONE\" NEWFILEUID=\"NONE\"?>" + eol + eol;
            }
            else
            {
                header =
                    "OFXHEADER:100" + eol +
                    "DATA:OFXSGML" + eol +
                    "VERSION:102" + eol +
                    "SECURITY:" + (encrypt ? "TYPE1" : "NONE") + eol +
                    "ENCODING:USASCII" + eol +
                    "CHARSET:1252" + eol +
                    "COMPRESSION:NONE" + eol +
                    "OLDFILEUID:NONE" + eol +
                    "NEWFILEUID:NONE" + eol + eol;
                   // "NEWFILEUID:" + newFileId + eol + eol;
            }

            return header;
        }

        //
        // Message sets
        //
        public string GetProfileMessageSet()
        {
            var messageSet = new SgmlAggregate("OFX");
            messageSet.AddTag(GetSignonMessage("anonymous00000000000000000000000", "anonymous00000000000000000000000", false));
            messageSet.AddTag(GetProfileMessage());

            return GetStringFromMessageSet(messageSet);
        }

        public string GetChallengeMessageSet(string user)
        {
            var messageSet = new SgmlAggregate("OFX");
            messageSet.AddTag(GetSignonMessage("anonymous00000000000000000000000", "anonymous00000000000000000000000", false));
            messageSet.AddTag(GetChallengeTransaction(user));

            return GetStringFromMessageSet(messageSet);
        }

        public string GetAccountsMessageSet(string user, string password)
        {
            var request = GetAccountsRequest();
            var accountMessage = GetMessageWrapper("SIGNUP", "ACCTINFO", request);

            var messageSet = new SgmlAggregate("OFX");
            messageSet.AddTag(GetSignonMessage(user, password, false));
            messageSet.AddTag(accountMessage);

            return GetStringFromMessageSet(messageSet);
        }

        public string GetInvestmentsMessageSet(string user, string password, string account)
        {
            throw new NotImplementedException();
        }

        private string GetStringFromMessageSet(SgmlAggregate messageSet)
        {
            return GetHeader(false) + (financialInstitution.XML ? messageSet.ToXml() : messageSet.ToString());
        }

        //
        // Messages
        //
        private SgmlTag GetSignonMessage(string user, string password, bool challenge)
        {
            var message = new SgmlAggregate("SIGNONMSGSRQV1");
            message.Tags.Add(GetSignonRequest(user, password));
            if (challenge)
            {
                message.Tags.Add(GetChallengeTransaction(user));
            }

            return message;
        }

        private SgmlTag GetProfileMessage()
        {
            return GetMessageWrapper("PROF", "PROF", GetProfileRequest());
        }

        private SgmlTag GetMessageWrapper(string messageType, string transactionType, SgmlTag request)
        {
            var transaction = new SgmlAggregate(transactionType + "TRNRQ");
            transaction.Tags.Add(new SgmlElement("TRNUID", GetUID()));
            transaction.Tags.Add(new SgmlElement("CLTCOOKIE", GetCookie()));
            transaction.Tags.Add(request);

            var message = new SgmlAggregate(messageType + "MSGSRQV1", transaction);

            return message;
        }

        //
        // Transactions
        //

        private SgmlTag GetChallengeTransaction(string user)
        {
            var transaction = new SgmlAggregate("CHALLENGETRNRQ");
            transaction.AddElement("TRNUID", GetUID());
            transaction.AddTag(GetChallengeRequest(user));

            return transaction;
        }

        //
        // Requests
        //

        public SgmlTag GetSignonRequest(string user, string password)
        {
            var request = new SgmlAggregate("SONRQ");
            request.AddElement("DTCLIENT", GetDate());
            request.AddElement("USERID", user);
            request.AddElement("USERPASS", password);
            request.AddElement("GENUSERKEY", "N");
            request.AddElement("LANGUAGE", "ENG");
            request.AddTag(GetFiInfo());
            request.AddElement("APPID", "QWIN");
            request.AddElement("APPVER", "2700");

            return request;
        }

        private SgmlTag GetChallengeRequest(string user)
        {
            var request = new SgmlAggregate("CHALLENGERQ");
            request.AddElement("USERID", user);

            return request;
        }

        private SgmlTag GetProfileRequest()
        {
            var request = new SgmlAggregate("PROFRQ");
            request.AddElement("CLIENTROUTING", "MSGSET");
            request.AddElement("DTPROFUP", "19900101");

            return request;
        }

        private SgmlTag GetAccountsRequest()
        {
            string oneMonthAgo = DateTime.Now.AddDays(-31).ToString("yyyymmddHHmmss");

            var request = new SgmlAggregate("ACCTINFORQ");
            //request.AddElement("DTACCTUP", oneMonthAgo);
            request.AddElement("DTACCTUP", "19700101000000");

            return request;
        }


        //
        // Utilities
        //
        private string GetDate()
        {
            //var date = DateTime.Now.ToString("yyyyMMddHHmmss");
            var date = DateTime.Now.ToString("yyyyMMddHHmmss.fff[z:EST]");

            return date;
        }

        private string GetCookie()
        {
            cookie += 1;

            return cookie.ToString();
        }

        private string GetUID()
        {
            var guid = Guid.NewGuid();
            string guidstr = guid.ToString("N").ToUpper();

            return guidstr;
        }

        private SgmlAggregate GetFiInfo()
        {
            var fiInfo = new SgmlAggregate("FI", new SgmlElement("ORG", financialInstitution.Organization));
            if (financialInstitution.FID != null)
            {
                fiInfo.AddElement("FID", financialInstitution.FID);
            }

            return fiInfo;
        }
    }

    // base class for sgml entities
    public abstract class SgmlTag
    {
        public readonly string Tag;

        public SgmlTag(string tag)
        {
            Tag = tag;
        }

        public abstract string ToXml();
    }

    // <tag>value
    public class SgmlElement : SgmlTag
    {
        public readonly string Value;

        public SgmlElement(string tag, string value)
            : base(tag)
        {
            Value = value;
        }

        public override string ToString()
        {
            return $"<{Tag}>{Value}\r\n";
        }

        public override string ToXml()
        {
            return $"<{Tag}>{Value}</{Tag}>\r\n";
        }
    }

    // <tag>element(s)</tag>
    public class SgmlAggregate : SgmlTag
    {
        public readonly List<SgmlTag> Tags;

        public SgmlAggregate(string tag)
            : base(tag)
        {
            Tags = new List<SgmlTag>();
        }

        public SgmlAggregate(string tag, SgmlTag value)
            : this(tag)
        {
            Tags.Add(value);
        }

        public SgmlAggregate(string tag, SgmlTag val1, SgmlTag val2)
            : this(tag)
        {
            Tags.Add(val1);
            Tags.Add(val2);
        }

        public void AddTag(SgmlTag tag)
        {
            Tags.Add(tag);
        }

        public void AddElement(string tag, string value)
        {
            Tags.Add(new SgmlElement(tag, value));
        }

        public override string ToString()
        {
            string result;

            result = $"<{Tag}>\r\n";

            foreach(var tag in Tags)
            {
                result += tag.ToString();
            }

            result += $"</{Tag}>\r\n";

            return result;
        }

        public override string ToXml()
        {
            string result;

            result = $"<{Tag}>\r\n";

            foreach (var tag in Tags)
            {
                result += tag.ToXml();
            }

            result += $"</{Tag}>\r\n";

            return result;
        }
    }

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

    public class FinancialInstitution
    {
        // Name of the institution
        public readonly string Organization;

        // Financial Institution Id
        public readonly string FID;

        // OFX URL
        public readonly string URL;

        // Banking available
        public readonly bool IsBank;

        // Invstement available
        public readonly bool IsInvestment;

        // Can we get an account list
        public readonly bool CanAccountList;

        // Encoded password
        public readonly bool EncryptedPassword;

        // OFX 2.0 or greater
        public readonly bool XML;

        public FinancialInstitution(string organization, string fid, string url, bool isBank, bool isInvestment, bool canAccountList, bool encodedPassword, bool xml)
        {
            Organization = organization;
            FID = fid;
            URL = url;
            IsBank = isBank;
            IsInvestment = isInvestment;
            CanAccountList = canAccountList;
            EncryptedPassword = encodedPassword;
            XML = xml;
        }

        static public readonly Dictionary<string, FinancialInstitution> FinancialInstitutions;
        static FinancialInstitution()
        {
            FinancialInstitutions = new Dictionary<string, FinancialInstitution>();
            FinancialInstitutions["Test"] = new FinancialInstitution(
                "Test", "6666", "http://127.0.0.1/go-ofx", false, true, true, false, false);
            FinancialInstitutions["Reference"] = new FinancialInstitution(
                "ReferenceFI", "00000", "https://ofx.innovision.com", true, true, true, false, true);
            FinancialInstitutions["AFS"] = new FinancialInstitution(
                "INTUIT", "7779", "https://ofx3.financialtrans.com/tf/OFXServer?tx=OFXController&cz=702110804131918&cl=50900132018", false, true, true, false, false);
            FinancialInstitutions["Vanguard"] = new FinancialInstitution(
                "vanguard.com", "15103", "https://vesnc.vanguard.com/us/OfxDirectConnectServlet", false, true, true, false, true);
            FinancialInstitutions["Vanguard2"] = new FinancialInstitution(
                "vanguard.com", null, "https://vesnc.vanguard.com/us/OfxDirectConnectServlet", false, true, true, false, true);
            FinancialInstitutions["Fidelity"] = new FinancialInstitution(
                "fidelity.com", "7776", "https://ofx.fidelity.com/ftgw/OFX/clients/download", false, true, true, false, false);
            FinancialInstitutions["Chase"] = new FinancialInstitution(
                "B1", "10898", "https://ofx.chase.com", true, false, true, false, true);
        }
    }


}
