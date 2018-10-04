using iTextSharp.text.pdf;

namespace DocumentSignerApi.Models
{
    public interface IAuthenticatedAttributes
    {
        byte[] DigestedData { get; set; }
        PdfSignatureAppearance PdfSignatureAppearance { get; set; }
        byte[] Value { get; set; }
        string ValueBase64 { get; }
    }
}
