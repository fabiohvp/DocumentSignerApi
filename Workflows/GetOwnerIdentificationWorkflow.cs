using DocumentSignerApi.Helpers;
using System.Security.Cryptography.X509Certificates;

namespace DocumentSignerApi.Workflows
{
    public class GetOwnerIdentificationWorkflow : GetCertificatePropertyWorkflow<string>
    {
        protected override string ExecuteWorkflow(X509Certificate2 candidate)
        {
            var oidValue = OidHelper.GetOidValue(candidate, OidHelper.Personal);

            if (oidValue != null)
            {
                return oidValue.Substring(8, 11);
            }

            oidValue = OidHelper.GetOidValue(candidate, OidHelper.Company);

            if (oidValue != null)
            {
                return oidValue.Substring(8, 11);
            }

            return oidValue;
        }
    }
}