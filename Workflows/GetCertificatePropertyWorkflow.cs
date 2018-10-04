using Enflow.Base;
using System;
using System.Security.Cryptography.X509Certificates;

namespace DocumentSignerApi.Workflows
{
    public abstract class GetCertificatePropertyWorkflow<T> : Workflow<X509Certificate2, T>
    {
        public T Execute(string certificadoBase64)
        {
            var certificado = certificadoBase64
                .ToX509Certificate2();

            return Execute(certificado);
        }
    }
}
