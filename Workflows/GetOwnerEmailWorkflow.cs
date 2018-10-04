using Org.BouncyCastle.Security;
using System.Collections;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace DocumentSignerApi.Workflows
{
    public class GetOwnerEmailWorkflow : GetCertificatePropertyWorkflow<string>
    {
        protected override string ExecuteWorkflow(X509Certificate2 candidate)
        {
            var certificate = DotNetUtilities.FromX509Certificate(candidate);

            if (certificate.GetSubjectAlternativeNames() != null)
            {
                var alternativeNames = certificate
                    .GetSubjectAlternativeNames()
                    .Cast<ArrayList>()
                    .Where(o => o.Count == 2 && o[0].GetType() == typeof(int));

                var oidKeys = alternativeNames
                    .Where(o => o[1] != null)
                    .Select(o => o[1].ToString());

                foreach (var oidValue in oidKeys)
                {
                    if (oidValue.Contains("@"))
                    {
                        return oidValue;
                    }
                }
            }

            return default(string);
        }
    }
}