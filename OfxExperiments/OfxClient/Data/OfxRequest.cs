using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfxClient.Data
{
    public enum EAction { FinancialInstitutionInformation, Profile, Accounts, Investments  };

    public abstract class OfxRequest
    {
        public readonly FinancialInstitution FinancialInstitution;
        public string URL { get; protected set; }

        public OfxRequest(string financialInstitutionId)
        {
            FinancialInstitution = FinancialInstitution.FinancialInstitutions[financialInstitutionId];

            // By default we setup the URL as the profile URL. This may be overriden in derived classes
            URL = FinancialInstitution.ProfileURL;
        }
    }

    public class FinancialInstitutionInformationRequest : OfxRequest
    {
        public readonly string IdOfInstitutionToCheck;

        public FinancialInstitutionInformationRequest(string IntuitId, string idOfInstitutionToCheck)
            : base(IntuitId)
        {
            IdOfInstitutionToCheck = idOfInstitutionToCheck;
        }
    }

    public class ProfileRequest : OfxRequest
    {
        public ProfileRequest(string financialInstitutionId)
            : base(financialInstitutionId)
        {
        }
    }

    public class AccountsRequest : OfxRequest
    {
        public readonly string User;
        public readonly string Password;

        public AccountsRequest(string financialInstitutionId, string url, string user, string password)
            : base(financialInstitutionId)
        {
            URL = url;
            User = user;
            Password = password;
        }
    }
}
