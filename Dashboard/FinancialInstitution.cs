using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard
{
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

        // Investement available
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
                "Vanguard", "15103", "https://vesnc.vanguard.com/us/OfxDirectConnectServlet", false, true, true, false, false);
            FinancialInstitutions["Vanguard2"] = new FinancialInstitution(
                "Vanguard", null, "https://vesnc.vanguard.com/us/OfxDirectConnectServlet", false, true, true, false, true);
            FinancialInstitutions["Fidelity"] = new FinancialInstitution(
                "fidelity.com", "7776", "https://ofx.fidelity.com/ftgw/OFX/clients/download", false, true, true, false, false);
            FinancialInstitutions["Chase"] = new FinancialInstitution(
                "B1", "10898", "https://ofx.chase.com", true, false, true, false, true);
        }
    }
}
