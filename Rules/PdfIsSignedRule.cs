using Enflow.Base;
using DocumentSignerApi.Helpers;
using System;

namespace DocumentSignerApi.Rules
{
    public class PdfIsSignedRule : StateRule<byte[]>
    {
        public PdfIsSignedRule()
        {
            Description = "Documento possui assinatura";
        }

        public override System.Linq.Expressions.Expression<Func<byte[], bool>> Predicate
        {
            get
            {
                return pdf => IsSigned(pdf);
            }
        }

        private static bool IsSigned(byte[] pdfContents)
        {
            using (var pdf = new PdfHelper(pdfContents))
            {
                return pdf.IsSigned;
            }
        }
    }
}
