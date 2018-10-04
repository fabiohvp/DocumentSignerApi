using Enflow.Base;
using DocumentSignerApi.Helpers;
using System;
using System.Linq;

namespace DocumentSignerApi.Rules
{
    public class PdfIsContentModifiedAfterSigningRule : StateRule<byte[]>
    {
        public PdfIsContentModifiedAfterSigningRule()
        {
            Description = "Houve modificação após o documento ser assinado";
        }

        public override System.Linq.Expressions.Expression<Func<byte[], bool>> Predicate
        {
            get
            {
                return fileContents =>
                    PdfHelper.GetSignatures(fileContents)
                        .AsParallel()
                        .Any(signature => PdfHelper.VerifySignatureContentModified(fileContents, signature.SignatureFieldName));
            }
        }
    }
}
