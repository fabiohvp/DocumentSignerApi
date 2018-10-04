using Enflow.Base;
using System;
using System.Security.Cryptography.X509Certificates;

namespace DocumentSignerApi.Rules
{
    public class IsCertificateExpiredRule : StateRule<X509Certificate2>
    {
        public IsCertificateExpiredRule()
        {
            Description = "Certificado expirou ou não pode ser utilizado ainda";
        }

        public override System.Linq.Expressions.Expression<Func<X509Certificate2, bool>> Predicate
        {
            get
            {
                return certificate => certificate.NotBefore < DateTime.Now && certificate.NotAfter > DateTime.Now;
            }
        }
    }
}
