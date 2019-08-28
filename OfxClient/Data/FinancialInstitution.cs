using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfxClient.Data
{
    public class FinancialInstitution
    {
        // Username/password to use for anonymous transactions
        // "anonymous" padded with 23 zeros to 32 chars
        public const string ANONYMOUS = "anonymous00000000000000000000000";

        // Name of the institution
        public readonly string Organization;

        // Financial Institution Id
        public readonly string FID;

        // OFX URL
        // To ask for profile
        public readonly string ProfileURL;

        // To send password challengr
        public string ChallengeURL;


        // Banking available
        public readonly bool IsBank;

        // Investement available
        public readonly bool IsInvestment;

        // Can we get an account list
        public readonly bool CanAccountList;

        // Encoded password
        public readonly bool EncryptedPassword;

        // OFX 2.0 or greater
        public readonly bool XML;

        public FinancialInstitution(string organization, string fid, string profileURL, bool isBank, bool isInvestment, bool canAccountList, bool encodedPassword, bool xml)
        {
            Organization = organization;
            FID = fid;
            ProfileURL = profileURL;
            IsBank = isBank;
            IsInvestment = isInvestment;
            CanAccountList = canAccountList;
            EncryptedPassword = encodedPassword;
            XML = xml;
        }

        public bool IsSameInstitution(string organization, string fid, string profileURL)
        {
            return organization == Organization && fid == FID && profileURL == ProfileURL;
        }

        static public readonly Dictionary<string, FinancialInstitution> FinancialInstitutions;
        static FinancialInstitution()
        {
            FinancialInstitutions = new Dictionary<string, FinancialInstitution>();
            // Test listener
            FinancialInstitutions["6666"] = new FinancialInstitution(
                "Test", "6666", "http://my.benoit.com/", false, true, true, false, false);
            // Reference fake institution (does not work)
            FinancialInstitutions["Reference"] = new FinancialInstitution(
                "ReferenceFI", "00000", "https://ofx.innovision.com", true, true, true, false, true);
            // American funds
            FinancialInstitutions["7779"] = new FinancialInstitution(
                "INTUIT", "7779", "https://ofx3.financialtrans.com/tf/OFXServer?tx=OFXController&cz=702110804131918&cl=50900132018", false, true, true, false, false);
            // Vanguard
            FinancialInstitutions["15103"] = new FinancialInstitution(
                "Vanguard", "15103", "https://vesnc.vanguard.com/us/OfxProfileServlet", false, true, true, false, false);
            // Fidelity
            FinancialInstitutions["7776"] = new FinancialInstitution(
                "fidelity.com", "7776", "https://ofx.fidelity.com/ftgw/OFX/clients/download", false, true, true, false, false);
            // Chase
            FinancialInstitutions["10898"] = new FinancialInstitution(
                "B1", "10898", "https://ofx.chase.com", true, false, true, false, true);
            // Intuit itself, to get FI information
            FinancialInstitutions["Intuit"] = new FinancialInstitution(
                null, null, "https://ofx-prod-brand.intuit.com/qw2800/fib.dll", false, false, false, false, false);
        }
    }
}
