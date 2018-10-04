using Enflow.Base;
using DocumentSignerApi.Helpers;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace DocumentSignerApi.Rules
{
    public class PdfIsLastSignatureCoversWholeDocumentRule : StateRule<byte[]>
    {
        public PdfIsLastSignatureCoversWholeDocumentRule()
        {
            Description = "Última assinatura cobre todo o documento";
        }

        public override Expression<Func<byte[], bool>> Predicate
        {
            get
            {
                return fileContents =>
                    PdfHelper.VerifySignatureCoversWholeDocument(fileContents, PdfHelper.GetSignatures(fileContents).LastOrDefault().SignatureFieldName);
            }
        }
    }
}
