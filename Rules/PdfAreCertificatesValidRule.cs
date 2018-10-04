using DocumentSignerApi.Helpers;
using Enflow.Base;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace DocumentSignerApi.Rules
{
    public class PdfAreCertificatesValidRule : StateRule<byte[]>
    {
        public PdfAreCertificatesValidRule()
        {
            Description = "Os certificados utilizados nas assinaturas são válidos";
        }

        public override Expression<Func<byte[], bool>> Predicate
        {
            get
            {
                return fileContents =>
                    !PdfHelper.GetSignatures(fileContents)
                        .AsParallel()
                        .Any(signature =>
                            !new IsCertificateValidRule(signature.SignDate)
                                .IsSatisfied(signature.Certificate)
                        );
            }
        }
    }
}
