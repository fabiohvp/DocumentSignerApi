using Enflow.Base;
using System;

namespace DocumentSignerApi.Rules
{
    public class PdfAreSignaturesValidRule : StateRule<byte[]>
    {
        PdfIsContentModifiedAfterSigningRule PdfIsContentModifiedAfterSigningRule;
        PdfIsLastSignatureCoversWholeDocumentRule PdfIsLastSignatureCoversWholeDocumentRule;

        public PdfAreSignaturesValidRule()
        {
            Description = "Não houve modificação após o documento ser assinado e a última assinatura cobre todo o documento";
            PdfIsContentModifiedAfterSigningRule = new PdfIsContentModifiedAfterSigningRule();
            PdfIsLastSignatureCoversWholeDocumentRule = new PdfIsLastSignatureCoversWholeDocumentRule();
        }

        public override System.Linq.Expressions.Expression<Func<byte[], bool>> Predicate
        {
            get
            {
                return PdfIsLastSignatureCoversWholeDocumentRule
                    .Or(PdfIsContentModifiedAfterSigningRule.Not())
                    .Predicate;
            }
        }
    }
}