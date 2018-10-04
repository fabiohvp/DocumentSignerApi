using DocumentSignerApi.Helpers;
using Org.BouncyCastle.Asn1.X509;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace DocumentSignerApi.Workflows
{
    public class GetOwnerNameWorkflow : GetCertificatePropertyWorkflow<string>
    {
        protected override string ExecuteWorkflow(X509Certificate2 candidate)
        {
            var name = OidHelper.GetSubjectByOid(candidate, X509Name.Name.Id) ?? OidHelper.GetSubjectByOid(candidate, X509Name.CN.Id);

            if (name == null)
            {
                return OidHelper.OwnerIdentificationWorkflow.Execute(candidate);
            }

            return name
                .Split(new char[] { ':', '-' })
                .FirstOrDefault();
        }
    }
}