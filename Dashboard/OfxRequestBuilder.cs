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
                    "OFXHEADER:100" + eol +
                    "DATA:OFXSGML" + eol +
                    "VERSION:102" + eol +
                    "SECURITY:" + (encrypt ? "TYPE1" : "NONE") + eol +
                    "ENCODING:USASCII" + eol +
                    "CHARSET:1252" + eol +
                    "COMPRESSION:NONE" + eol +
                    "OLDFILEUID:NONE" + eol +
                    "NEWFILEUID:NONE" + eol + eol;

                bool putInXmlComment = false; // ZZZ
                if (putInXmlComment)
                {
                    header =
                        "<!--" + eol +
                        header +
                        "-->" + eol;
                }
            }

            return header;
        }

        //
        // Message sets
        //

        // Get profile (i.e. capabilities) of a financial institution
        public string GetProfileMessageSet()
        {
            var anonymous = FinancialInstitution.ANONYMOUS;
            var messageSet = new SgmlAggregate("OFX");
            messageSet.AddTag(GetSignonMessage(anonymous, anonymous, false));
            messageSet.AddTag(GetProfileMessage());

            return GetStringFromMessageSet(messageSet);
        }

        // To encrypt password (not used)
        public string GetChallengeMessageSet(string user)
        {
            var anonymous = FinancialInstitution.ANONYMOUS;
            var messageSet = new SgmlAggregate("OFX");
            messageSet.AddTag(GetSignonMessage(anonymous, anonymous, false));
            messageSet.AddTag(GetChallengeTransaction(user));

            return GetStringFromMessageSet(messageSet);
        }

        // To get account list
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

        // To get information about a financial institution (Intuit-specific)
        public string GetFinancialInstitutionInformationMessageSet(string financialInstutionId)
        {
            var anonymous = FinancialInstitution.ANONYMOUS;
            var messageSet = new SgmlAggregate("OFX");
            messageSet.AddTag(GetSignonMessage(anonymous, anonymous, false));
            messageSet.AddTag(GetFinancialInstitutionInformationMessage(financialInstutionId));

            return GetStringFromMessageSet(messageSet);
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

        private SgmlTag GetFinancialInstitutionInformationMessage(string financialInstutionId)
        {
            return GetMessageWrapper("INTU.BRAND", "INTU.BRAND", GetFinancialInstitutionInformationRequest(financialInstutionId));
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
            var fiInfo = GetFiInfo();
            if (fiInfo != null)
            {
                request.AddTag(fiInfo);
            }
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

        private SgmlTag GetFinancialInstitutionInformationRequest(string financialInstutionId)
        {
            /*
            <INTU.BRANDRQ>
            <INTU.SW>
            <INTU.SWPRODCD>Q
            <INTU.SWVER>1000
            <INTU.SWOSCD>W
            </INTU.SW>
            <INTU.SELECTBYBID>
            <INTU.BIDINFO>
            <INTU.PRESENCEID>15103
            <INTU.DTUPDATE>19900101
            <INTU.QUOTESID>C2A36156F5E60D00FC46350B2D43CDAB
            </INTU.BIDINFO>
            </INTU.SELECTBYBID>
            </INTU.BRANDRQ>
            */

            var sw = new SgmlAggregate("INTU.SW");
            sw.AddElement("INTU.SWPRODCD", "Q");    // I suppose this means Quicken
            sw.AddElement("INTU.SWVER", "1000");    // I suppose this means latest version
            sw.AddElement("INTU.SWOSCD", "W");      // I suppose this means runs on Windows

            var bidInfo = new SgmlAggregate("INTU.BIDINFO");
            bidInfo.AddElement("INTU.PRESENCEID", financialInstutionId);
            bidInfo.AddElement("INTU.DTUPDATE", "19900101");
            bidInfo.AddElement("INTU.PRESENCEID", "C2A36156F5E60D00FC46350B2D43CDAB"); // ZZZ ???

            var selectById = new SgmlAggregate("INTU.SELECTBYBID");
            selectById.Tags.Add(bidInfo);

            var brandrq = new SgmlAggregate("INTU.BRANDRQ");
            brandrq.Tags.Add(sw);
            brandrq.Tags.Add(selectById);

            return brandrq;
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
            SgmlAggregate fiInfo = null;

            if (FinancialInstitution.Organization != null)
            {
                fiInfo = new SgmlAggregate("FI", new SgmlElement("ORG", FinancialInstitution.Organization));
                if (FinancialInstitution.FID != null)
                {
                    fiInfo.AddElement("FID", FinancialInstitution.FID);
                }
            }

            return fiInfo;
        }
    }


}
