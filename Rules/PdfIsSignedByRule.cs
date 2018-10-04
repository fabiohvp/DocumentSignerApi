using DocumentSignerApi.Helpers;
using Enflow.Base;
using System;
using System.Linq;

namespace DocumentSignerApi.Rules
{
    public class PdfIsSignedByRule : StateRule<byte[]>
    {
        public string Identification;

        public PdfIsSignedByRule(string identification)
        {
            Description = "Document is signed by " + identification;
            Identification = identification;
        }

        public override System.Linq.Expressions.Expression<Func<byte[], bool>> Predicate
        {
            get
            {
                return pdfContents => PdfHelper.GetSignatures(pdfContents)
                    .Any(o => OidHelper.OwnerIdentificationWorkflow.Execute(o.Certificate) == Identification);
            }
        }
    }
}
