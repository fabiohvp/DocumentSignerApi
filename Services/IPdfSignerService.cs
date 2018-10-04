using DocumentSignerApi.Models;
using iTextSharp.text.pdf;

namespace DocumentSignerApi.Services
{
    public interface IPdfSignerService
    {
        ITokenService TokenService { get; }

        IAuthenticatedAttributes CreateAuthenticatedAttribute(IPdfPreSigningSession sessionData);

        void SignData(IPdfSigningSession sessionData, string signedHashBase64);

        void UpdatePdfSignatureAppearance(IPdfPreSigningSession sessionData, PdfReader pdfReader, PdfSignatureAppearance pdfSignatureAppearance);
    }
}
