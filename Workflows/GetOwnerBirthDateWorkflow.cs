using DocumentSignerApi.Helpers;
using System;
using System.Security.Cryptography.X509Certificates;

namespace DocumentSignerApi.Workflows
{
    public class GetOwnerBirthDateWorkflow : GetCertificatePropertyWorkflow<DateTime>
    {
        protected override DateTime ExecuteWorkflow(X509Certificate2 candidate)
        {
            var dateString = default(string);
            var oidValue = OidHelper.GetOidValue(candidate, OidHelper.Personal);

            if (oidValue != null)
            {
                dateString = oidValue.Substring(0, 8);
            }

            oidValue = OidHelper.GetOidValue(candidate, OidHelper.Company);

            if (oidValue != null)
            {
                dateString = oidValue.Substring(0, 8);
            }

            dateString = dateString.Insert(2, "/");
            dateString = dateString.Insert(5, "/");
            return DateTime.ParseExact(dateString, "dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("pt-BR"));
        }
    }
}