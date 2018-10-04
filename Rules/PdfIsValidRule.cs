using Enflow.Base;
using System;

namespace DocumentSignerApi.Rules
{
    public class PdfIsValidRule : StateRule<byte[]>
    {
        private PdfIsSignedRule PdfIsSignedRule;
        private PdfAreCertificatesValidRule PdfAreCertificatesValidRule;
        private PdfAreSignaturesValidRule PdfAreSignaturesValidRule;

        public PdfIsValidRule()
        {
            Description = "Certificados e assinaturas válidas";
            PdfAreCertificatesValidRule = new PdfAreCertificatesValidRule();
            PdfIsSignedRule = new PdfIsSignedRule();
            PdfAreSignaturesValidRule = new PdfAreSignaturesValidRule();
        }

        public override System.Linq.Expressions.Expression<Func<byte[], bool>> Predicate
        {
            get
            {
                return PdfIsSignedRule
                    .Not()
                    .Or(
                        PdfIsSignedRule
                            .And(PdfAreCertificatesValidRule)
                            .And(PdfAreSignaturesValidRule)
                    )
                    .Predicate;
            }
        }
    }
}
