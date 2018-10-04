using Enflow.Base;
using DocumentSignerApi.Helpers;
using System;

namespace DocumentSignerApi.Rules
{
    public class PdfIsFormatARule : StateRule<byte[]>
    {
        public PdfIsFormatARule()
        {
            Description = "Documento está no formato PDF/A";
        }

        public override System.Linq.Expressions.Expression<Func<byte[], bool>> Predicate
        {
            get
            {
                return pdf => IsPdfA(pdf);
            }
        }

        private static bool IsPdfA(byte[] pdfContents)
        {
            using (var pdf = new PdfHelper(pdfContents))
            {
                return pdf.IsPdfA;
            }
        }
    }
}
