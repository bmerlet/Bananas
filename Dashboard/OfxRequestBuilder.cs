using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard
{
    public class OfxRequestBuilder
    {
        public readonly FinancialInstitution FinancialInstitution;

        private const string eol = "\r\n";

        private readonly string newFileId;

        private string transactionId;

        private int cookie = 0;

        // Constructor
        public OfxRequestBuilder(FinancialInstitution financialInstitution)
        {
            this.FinancialInstitution = financialInstitution;
            newFileId = GenerateNewTransactionId();
            UpdateTransactionId();
        }

        // If need to change transaction Id
        public void UpdateTransactionId()
        {
            transactionId = GenerateNewTransactionId();
        }

        //
        // Header
        //
        public string GetHeader(bool encrypt)
        {
            string header;

            if (FinancialInstitution.XML)
            {
                header =
                    "<?xml version=\"1.0\">" + eol + eol +
                    "<?OFX OFXHEADER=\"200\" VERSION=\"220\" SECURITY=\"NONE\" OLDFILEUID=\"NONE\" NEWFILEUID=\"NONE\"?>" + eol + eol;
            }
            else
            {
                header =
                    "<!--" + eol +
                    "OFXHEADER:100" + eol +
                    "DATA:OFXSGML" + eol +
                    "VERSION:102" + eol +
                    "SECURITY:" + (encrypt ? "TYPE1" : "NONE") + eol +
                    "ENCODING:USASCII" + eol +
                    "CHARSET:1252" + eol +
                    "COMPRESSION:NONE" + eol +
                    "OLDFILEUID:NONE" + eol +
                    "NEWFILEUID:NONE" + eol + eol +
                    "-->" + eol;
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
            return GetHeader(false) + (FinancialInstitution.XML ? messageSet.ToXml() : messageSet.ToString());
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
            transaction.Tags.Add(new SgmlElement("TRNUID", transactionId));
            //transaction.Tags.Add(new SgmlElement("CLTCOOKIE", GetCookie()));
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
            transaction.AddElement("TRNUID", transactionId);
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
            request.AddElement("DTACCTUP", "19900101");

            return request;
        }


        //
        // Utilities
        //
        private string GetDate()
        {
            //var date = DateTime.Now.ToString("yyyyMMddHHmmss");
            var date = DateTime.Now.ToString("yyyyMMddHHmmss.fff[z:EDT]");

            return date;
        }

        private string GetCookie()
        {
            cookie += 1;

            return cookie.ToString();
        }

        public string GenerateNewTransactionId()
        {
            var guid = Guid.NewGuid();
            string guidstr = guid.ToString("D").ToUpper();

            return guidstr;
        }

        private SgmlAggregate GetFiInfo()
        {
            var fiInfo = new SgmlAggregate("FI", new SgmlElement("ORG", FinancialInstitution.Organization));
            if (FinancialInstitution.FID != null)
            {
                fiInfo.AddElement("FID", FinancialInstitution.FID);
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
        public readonly bool NoNewLine;

        public SgmlElement(string tag, string value, bool noNewLine = false)
            : base(tag)
        {
            Value = value;
            NoNewLine = noNewLine;
        }

        public override string ToString()
        {
            return $"<{Tag}>{Value}" + (NoNewLine ? "" : "\r\n");
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

        public void AddElement(string tag, string value, bool noNewLine = false)
        {
            Tags.Add(new SgmlElement(tag, value, noNewLine));
        }

        public override string ToString()
        {
            string result;

            result = $"<{Tag}>\r\n";

            foreach (var tag in Tags)
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

}
