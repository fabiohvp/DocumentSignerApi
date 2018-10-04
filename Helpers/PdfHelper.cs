using DocumentSignerApi.Exceptions;
using DocumentSignerApi.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using iTextSharp.text.xml.xmp;
using iTextSharp.xmp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DocumentSignerApi.Helpers
{
    public class PdfHelper : IDisposable
    {
        public const string EXTENSAO_PDF = ".pdf";

        /// <summary>
        /// Tem que ser utilizado dentro do using(var pdfReader = GetPdfReader())
        /// </summary>
        private bool IsPdf;
        private byte[] PdfContents;
        protected bool? _PdfA;
        protected bool? _Searchable;

        public PdfHelper(byte[] pdfContents)
        {
            PdfContents = pdfContents;
        }

        public PdfHelper(string filePath)
            : this(File.ReadAllBytes(filePath))
        {
        }

        private PdfReader GetPdfReader()
        {
            try
            {
                var pdfReader = new PdfReader(PdfContents);
                IsPdf = true;
                return pdfReader;
            }
            catch (Exception ex)
            {
                IsPdf = false;

                if (ex.Message == "Bad user password")
                {
                    throw new PdfProtectedException(ex);
                }
                else if (ex.Message == "PDF header signature not found")
                {
                    throw new PdfSignatureException(ex);
                }

                throw new IOException("Não foi possível ler o arquivo PDF", ex);
            }
        }

        public virtual string GetText()
        {
            var output = new StringBuilder();

            using (var pdfReader = GetPdfReader())
            {
                if (!IsPdf)
                {
                    return null;
                }

                for (var i = 1; i <= pdfReader.NumberOfPages; i++)
                {
                    var text = PdfTextExtractor.GetTextFromPage(pdfReader, i, new SimpleTextExtractionStrategy());
                    output.AppendLine(text);
                }
            }

            return output.ToString();
        }

        public virtual long Length
        {
            get
            {
                using (var pdfReader = GetPdfReader())
                {
                    if (!IsPdf)
                    {
                        return 0;
                    }

                    return pdfReader.FileLength;
                }
            }
        }

        public virtual bool IsPdfA
        {
            get
            {
                if (_PdfA == null)
                {
                    var pdfA = false;

                    using (var pdfReader = GetPdfReader())
                    {
                        if (!IsPdf)
                        {
                            return false;
                        }

                        if (pdfReader.Metadata != null)
                        {
                            var metadata = Encoding.Default.GetString(pdfReader.Metadata)
                                .ToLower()
                                .Replace("'", "\"");

                            pdfA = metadata.Contains("<pdfaid:conformance>a</pdfaid:conformance>")
                                || metadata.Contains("<pdfaid:conformance>b</pdfaid:conformance>")
                                || metadata.Contains("pdfaid:conformance=\"a\"")
                                || metadata.Contains("pdfaid:conformance=\"b\"");
                        }
                    }

                    _PdfA = pdfA;
                }

                return _PdfA.Value;
            }
        }

        public virtual bool IsSearchable
        {
            get
            {
                if (_Searchable == null)
                {
                    var pesquisavel = false;

                    using (var pdfReader = GetPdfReader())
                    {
                        if (!IsPdf)
                        {
                            return false;
                        }

                        for (var i = 1; i <= pdfReader.NumberOfPages; i++)
                        {
                            var its = new LocationTextExtractionStrategy();

                            try
                            {
                                if (!string.IsNullOrEmpty(PdfTextExtractor.GetTextFromPage(pdfReader, i, its)))
                                {
                                    pesquisavel = true;
                                    break;
                                }
                            }
                            catch (ArgumentException e)
                            {
                                if (e.Message.Contains("Unexpected color space"))
                                    pesquisavel = true;
                                else
                                    throw;

                                break;
                            }
                        }
                    }

                    _Searchable = pesquisavel;
                }

                return _Searchable.Value;
            }
        }

        public virtual bool IsSigned
        {
            get
            {
                return Signatures.Any();
            }
        }

        public virtual IEnumerable<Signature> Signatures
        {
            get
            {
                using (var pdfReader = GetPdfReader())
                {
                    if (!IsPdf)
                    {
                        return new List<Signature>();
                    }

                    return pdfReader.GetSignatures();
                }
            }
        }

        public virtual IEnumerable<string> SignaturesIdentifiers
        {
            get
            {
                var identifiers = new List<string>();

                foreach (var signature in Signatures)
                {
                    var cpf = OidHelper
                        .OwnerIdentificationWorkflow
                        .Execute(signature.Certificate);
                    identifiers.Add(cpf);
                }

                return identifiers;
            }
        }

        public virtual int TotalPages
        {
            get
            {
                using (var pdfReader = GetPdfReader())
                {
                    if (!IsPdf)
                    {
                        return 0;
                    }

                    return pdfReader.NumberOfPages;
                }
            }
        }

        #region Static methods
        public static IEnumerable<Signature> GetSignatures(byte[] fileContents)
        {
            using (var pdf = new PdfHelper(fileContents))
            {
                return pdf.Signatures;
            }
        }

        public static IEnumerable<Signature> GetSignatures(string filePath)
        {
            using (var pdf = new PdfHelper(filePath))
            {
                return pdf.Signatures;
            }
        }

        public static IEnumerable<string> GetSignaturesIdentifiers(byte[] fileContents)
        {
            using (var pdf = new PdfHelper(fileContents))
            {
                return pdf.SignaturesIdentifiers;
            }
        }

        public static IEnumerable<string> GetSignaturesIdentifiers(string filePath)
        {
            using (var pdf = new PdfHelper(filePath))
            {
                return pdf.SignaturesIdentifiers;
            }
        }

        public static string GetText(byte[] fileContents)
        {
            using (var pdf = new PdfHelper(fileContents))
            {
                return pdf.GetText();
            }
        }

        public static string GetText(string filePath)
        {
            using (var pdf = new PdfHelper(filePath))
            {
                return pdf.GetText();
            }
        }

        public static int GetTotalPages(byte[] fileContents)
        {
            using (var pdf = new PdfHelper(fileContents))
            {
                return pdf.TotalPages;
            }
        }

        public static int GetTotalPages(string filePath)
        {
            using (var pdf = new PdfHelper(filePath))
            {
                return pdf.TotalPages;
            }
        }

        public static bool IsPdfFromContents(byte[] fileContents)
        {
            //https://stackoverflow.com/questions/2731917/how-to-detect-if-a-file-is-pdf-or-tiff/2731973#2731973
            var ms = new MemoryStream(fileContents);
            var sr = new StreamReader(ms);
            var buf = new char[5];

            sr.Read(buf, 0, 4);
            sr.Close();

            var hdr = buf[0].ToString()
                + buf[1].ToString()
                + buf[2].ToString()
                + buf[3].ToString()
                + buf[4].ToString();

            return hdr.ToUpper().StartsWith("%PDF");
        }

        public static byte[] Merge(List<byte[]> filesContents)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var document = new Document(PageSize.A4))
                {
                    using (var copyWriter = new PdfSmartCopy(document, memoryStream))
                    {
                        document.Open();
                        copyWriter.RegisterFonts();

                        foreach (var fileContent in filesContents)
                        {
                            using (var pdfReader = new PdfReader(fileContent))
                            {
                                copyWriter.AddDocument(pdfReader);
                            }
                        }

                        document.Close();
                        copyWriter.RegisterProperties();
                    }
                }

                return AddPdfAFlag(memoryStream.ToArray());
            }
        }

        public static void Merge(List<string> filesContents, string filePath)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                using (var document = new Document(PageSize.A4))
                {
                    using (var copyWriter = new PdfSmartCopy(document, fileStream))
                    {
                        document.Open();
                        copyWriter.RegisterFonts();

                        foreach (var fileContent in filesContents)
                        {
                            using (var pdfReader = new PdfReader(fileContent))
                            {
                                copyWriter.AddDocument(pdfReader);
                            }
                        }

                        document.Close();
                        copyWriter.RegisterProperties();
                    }
                }
            }

            AddPdfAFlag(filePath);
        }

        public static byte[] AddPdfAFlag(byte[] fileContents)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var pdfReader = new PdfReader(fileContents))
                {
                    using (var pdfStamper = new PdfStamper(pdfReader, memoryStream, '\0', true))
                    {
                        using (var pdfaMemoryStream = new MemoryStream())
                        {
                            var xmpWriter = new XmpWriter(pdfaMemoryStream, pdfStamper.MoreInfo);
                            xmpWriter.SetProperty(XmpConst.NS_PDFA_ID, PdfAProperties.PART, "3");
                            xmpWriter.SetProperty(XmpConst.NS_PDFA_ID, PdfAProperties.CONFORMANCE, "A");
                            xmpWriter.Close();
                            pdfStamper.XmpMetadata = pdfaMemoryStream.ToArray();
                        }
                    }
                }
                return memoryStream.ToArray();
            }
        }

        public static void AddPdfAFlag(string filePath)
        {
            var newFilePath = filePath.CombineFilePathWithGuid();

            using (var fileStream = new FileStream(newFilePath, FileMode.Create))
            {
                using (var pdfReader = new PdfReader(filePath))
                {
                    using (var pdfStamper = new PdfStamper(pdfReader, fileStream, '\0', true))
                    {
                        using (var pdfaMemoryStream = new MemoryStream())
                        {
                            var xmpWriter = new XmpWriter(pdfaMemoryStream, pdfStamper.MoreInfo);
                            xmpWriter.SetProperty(XmpConst.NS_PDFA_ID, PdfAProperties.PART, "3");
                            xmpWriter.SetProperty(XmpConst.NS_PDFA_ID, PdfAProperties.CONFORMANCE, "A");
                            xmpWriter.Close();
                            pdfStamper.XmpMetadata = pdfaMemoryStream.ToArray();
                        }
                    }
                }
            }

            File.Copy(newFilePath, filePath, true);
        }

        public static byte[] MergePage(byte[] sourceContents, byte[] destinationContents, int sourcePageNumber = 1, int destinationPageNumber = 1)
        {
            var onlyOnePage = false;

            using (var memoryStream = new MemoryStream())
            {
                using (var document = new Document(PageSize.A4))
                {
                    using (var sourceReader = new PdfReader(sourceContents))
                    {
                        using (var destinationReader = new PdfReader(destinationContents))
                        {
                            onlyOnePage = destinationReader.NumberOfPages == 1;

                            using (var copyWriter = PdfWriter.GetInstance(document, memoryStream))
                            {
                                document.AddLanguage("pt-BR");
                                document.AddCreationDate();

                                document.Open();
                                copyWriter.RegisterFonts();

                                var contents = copyWriter.DirectContent;
                                var pageSize = sourceReader.GetPageSizeWithRotation(sourcePageNumber);

                                document.SetPageSize(pageSize);
                                document.NewPage();

                                var sourcePage = copyWriter.GetImportedPage(sourceReader, sourcePageNumber);
                                var destinationPage = copyWriter.GetImportedPage(destinationReader, destinationPageNumber);

                                contents.AddTemplate(destinationPage, 0, 0);
                                contents.AddTemplate(sourcePage, 0, 0);

                                document.Close();
                                copyWriter.RegisterProperties();

                                sourceContents = memoryStream.ToArray();
                            }
                        }
                    }
                }
            }

            if (onlyOnePage)
            {
                return sourceContents;
            }

            destinationContents = DeletePage(destinationContents, destinationPageNumber);

            return Merge(new List<byte[]> { sourceContents, destinationContents });
        }

        public static byte[] DeletePage(byte[] fileContents, int page)
        {
            return DeletePages(fileContents, new int[] { page });
        }

        public static byte[] DeletePages(byte[] fileContents, int[] pages)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var document = new Document(PageSize.A4))
                {
                    using (var pdfReader = new PdfReader(fileContents))
                    {
                        pages = pages
                            .Where(o => o >= 1 && o <= pdfReader.NumberOfPages)
                            .Distinct()
                            .ToArray();

                        if (pages.Length < pdfReader.NumberOfPages)
                        {
                            using (var copyWriter = new PdfSmartCopy(document, memoryStream))
                            {
                                document.Open();
                                copyWriter.RegisterFonts();

                                var contents = copyWriter.DirectContent;

                                for (int i = 1; i <= pdfReader.NumberOfPages; i++)
                                {
                                    if (!pages.Contains(i))
                                    {
                                        var importedPage = copyWriter.GetImportedPage(pdfReader, i);

                                        copyWriter.AddPage(importedPage);
                                    }
                                }

                                document.Close();
                                copyWriter.RegisterProperties();

                                fileContents = memoryStream.ToArray();
                            }
                        }
                    }
                }
            }

            return fileContents;
        }

        public static byte[] AddPageNumber(byte[] fileContents)
        {
            using (var pdfReader = new PdfReader(fileContents))
            {
                using (var memoryStream = new MemoryStream())
                {
                    using (var stamper = new PdfStamper(pdfReader, memoryStream))
                    {
                        var pages = pdfReader.NumberOfPages;

                        for (var i = 1; i <= pages; i++)
                        {
                            var pageSize = pdfReader.GetPageSizeWithRotation(i);
                            var phrase = new Phrase(i + "/" + pages, new Font(Font.FontFamily.COURIER, 8f));
                            ColumnText.ShowTextAligned(stamper.GetOverContent(i), Element.ALIGN_RIGHT, phrase, pageSize.Right - 10f, pageSize.Top - 20f, 0f);
                        }
                    }

                    memoryStream.Flush();
                    return memoryStream.GetBuffer();
                }
            }
        }

        public static byte[] AddWatermarkAsImage(byte[] fileContents, string textWatermark, int fontSize = 72, int angle = 45)
        {
            using (var pdfReader = new PdfReader(fileContents))
            {
                using (var memoryStream = new MemoryStream())
                {
                    using (var stamper = new PdfStamper(pdfReader, memoryStream))
                    {
                        AddWatermarkAsImage(pdfReader, stamper, textWatermark, fontSize, angle);
                    }

                    memoryStream.Flush();

                    return memoryStream.GetBuffer();
                }
            }
        }

        public static void AddWatermarkAsImage(string filePath, string textWatermark, int fontSize = 72, int angle = 45)
        {
            var newFilePath = filePath.CombineFilePathWithGuid();

            using (var fileStream = new FileStream(newFilePath, FileMode.Create))
            {
                using (var pdfReader = new PdfReader(filePath))
                {
                    using (var stamper = new PdfStamper(pdfReader, fileStream, '\0', true))
                    {
                        AddWatermarkAsImage(pdfReader, stamper, textWatermark, fontSize, angle);
                    }
                }
            }

            File.Copy(newFilePath, filePath, true);
        }

        private static void AddWatermarkAsImage(PdfReader pdfReader, PdfStamper stamper, string textWatermark, int fontSize = 72, int angle = 45)
        {
            using (var font = new System.Drawing.Font(BaseFont.TIMES_ROMAN, fontSize, System.Drawing.GraphicsUnit.Pixel))
            {
                var image = ImageHelper.CreateBitmapFromText(textWatermark, font, 30);
                var times = pdfReader.NumberOfPages;
                var pdfImage = iTextSharp.text.Image.GetInstance(image, System.Drawing.Imaging.ImageFormat.Png);

                for (var i = 1; i <= times; i++)
                {
                    var pageSize = pdfReader.GetPageSizeWithRotation(i);

                    var width = (float)(pageSize.Width - (Math.Sin(angle) * pdfImage.Height + Math.Cos(angle) * pdfImage.Width)) / 2;
                    var height = (float)(pageSize.Height - (Math.Sin(angle) * pdfImage.Width + Math.Cos(angle) * pdfImage.Height)) / 2;

                    pdfImage.RotationDegrees = angle;
                    //TODO: Centralizar isto!
                    pdfImage.SetAbsolutePosition(width, height);

                    stamper.GetOverContent(i).AddImage(pdfImage);
                }
            }
        }

        public static byte[] AddWatermarkAsText(byte[] fileContents, string textWatermark, int fontSize = 72, int angle = 45)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var pdfReader = new PdfReader(fileContents))
                {
                    using (var stamper = new PdfStamper(pdfReader, memoryStream))
                    {
                        var baseFont = BaseFont.CreateFont(BaseFont.TIMES_ROMAN, BaseFont.CP1252, true);
                        var times = pdfReader.NumberOfPages;

                        for (var i = 1; i <= times; i++)
                        {
                            var contentByte = stamper.GetOverContent(i);
                            AddWaterMarkText(contentByte, textWatermark, baseFont, fontSize, angle, new BaseColor(System.Drawing.Color.Black), pdfReader.GetPageSizeWithRotation(i));
                        }

                        stamper.Close();
                    }

                    return memoryStream.ToArray();
                }
            }
        }

        public static bool VerifySignatureCoversWholeDocument(byte[] fileContents, string signatureFieldName)
        {
            using (var pdfReader = new PdfReader(fileContents))
            {
                var acroFields = pdfReader
                    .AcroFields;

                var signaturesNames = acroFields.GetSignatureNames();

                if (!signaturesNames.Contains(signatureFieldName))
                {
                    return false;
                }

                return acroFields
                    .SignatureCoversWholeDocument(signatureFieldName);
            }
        }

        public static bool VerifySignatureContentModified(byte[] fileContents, string signatureFieldName)
        {
            using (var pdfReader = new PdfReader(fileContents))
            {
                var acroFields = pdfReader
                   .AcroFields;

                var signaturesNames = acroFields.GetSignatureNames();

                if (!signaturesNames.Contains(signatureFieldName))
                {
                    return false;
                }

                return !acroFields
                    .VerifySignature(signatureFieldName)
                    .Verify();
            }
        }


        private static void AddWaterMarkText(PdfContentByte directContent, string textWatermark, BaseFont font, float fontSize, float angle, BaseColor color, Rectangle realPageSize)
        {
            var gstate = new PdfGState { FillOpacity = 0.2f, StrokeOpacity = 0.2f };

            directContent.SaveState();
            directContent.SetGState(gstate);
            directContent.SetColorFill(color);
            directContent.BeginText();
            directContent.SetFontAndSize(font, fontSize);

            var x = (realPageSize.Right + realPageSize.Left) / 2;
            var y = (realPageSize.Bottom + realPageSize.Top) / 2;

            directContent.ShowTextAligned(Element.ALIGN_CENTER, textWatermark, x, y, angle);
            directContent.EndText();
            directContent.RestoreState();
        }

        public static bool HavingHyperlinks(byte[] fileContents)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var pdfReader = new PdfReader(fileContents))
                {
                    using (var stamper = new PdfStamper(pdfReader, memoryStream))
                    {
                        var pages = pdfReader.NumberOfPages;

                        for (var i = 1; i <= pages; i++)
                        {
                            var pageDictionary = pdfReader.GetPageN(i);
                            var annotations = pageDictionary.GetAsArray(PdfName.ANNOTS);
                            //var newAnnotations = new PdfArray();

                            if (annotations != null)
                            {
                                for (int j = 0; j < annotations.Size; ++j)
                                {
                                    var annotDict = annotations.GetAsDict(j);
                                    //if (!PdfName.LINK.Equals(annotDict.GetAsName(PdfName.SUBTYPE)))
                                    if (PdfName.LINK.Equals(annotDict.GetAsName(PdfName.SUBTYPE)))
                                    {
                                        //newAnnotations.Add(annotations[j]);
                                        return true;
                                    }
                                }
                                //pageDictionary.Put(PdfName.ANNOTS, newAnnotations);
                            }
                        }
                    }

                    memoryStream.Flush();
                    return false;
                }
            }
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            PdfContents = null;
        }

        #endregion
    }

    public static class PdfWriterExtensions
    {
        /// <summary>
        /// Must use this before calling document.Open()
        /// </summary>
        /// <param name="writer"></param>
        /// <returns></returns>
        public static PdfWriter RegisterProperties(this PdfWriter writer)
        {
            writer.PDFXConformance = PdfWriter.PDFX32002;
            writer.PdfVersion = PdfWriter.VERSION_1_7;
            writer.SetTagged();
            writer.ViewerPreferences = PdfWriter.DisplayDocTitle;
            writer.InitialLeading = 12.5f;
            writer.CompressionLevel = PdfStream.BEST_COMPRESSION;
            //writer.SetFullCompression(); //Causa erro no padrão PDF_A_1*

            writer.CreateXmpMetadata();

            return writer;
        }

        /// <summary>
        /// Must use this after calling document.Open()
        /// </summary>
        /// <param name="writer"></param>
        /// <returns></returns>
        public static PdfWriter RegisterFonts(this PdfWriter writer)
        {
            FontFactory.RegisterDirectories();
            var icc = ICC_Profile.GetInstance((byte[])Properties.Resources.ResourceManager.GetObject("icc_profile"));

            writer.SetOutputIntents("Custom", "", "http://www.color.org", "sRGB IEC61966-2.1", icc);
            return writer;
        }
    }

    public static class PdfReaderExtensions
    {
        public static IEnumerable<Signature> GetSignatures(this PdfReader pdfReader)
        {
            var signatures = new List<Signature>();

            foreach (var name in pdfReader.AcroFields.GetSignatureNames())
            {
                var pdfPKCS7 = pdfReader.AcroFields.VerifySignature(name);
                var signature = new Signature(name, FileFormat.Pdf, pdfPKCS7.SigningCertificate, pdfPKCS7.SignDate);

                signatures.Add(signature);
            }

            return signatures;
        }
    }
}
