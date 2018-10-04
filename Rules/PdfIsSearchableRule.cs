using Enflow.Base;
using DocumentSignerApi.Helpers;
using System;

namespace DocumentSignerApi.Rules
{
    public class PdfIsSearchableRule : StateRule<byte[]>
    {
        public PdfIsSearchableRule()
        {
            Description = "Documento possui caracteres";
        }

        public override System.Linq.Expressions.Expression<Func<byte[], bool>> Predicate
        {
            get
            {
                return pdf => IsSearchable(pdf);
            }
        }

        private static bool IsSearchable(byte[] pdfContents)
        {
            using (var pdf = new PdfHelper(pdfContents))
            {
                return pdf.IsSearchable;
            }
        }
    }
}
