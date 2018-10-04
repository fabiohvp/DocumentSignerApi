using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace System
{
    public static class StringExtensions
    {
        public static string FromHexadecimal(this string hexadecimalString)
        {
            if (hexadecimalString == null)
            {
                return null;
            }

            var builder = new StringBuilder();

            for (int i = 0; i < hexadecimalString.Length / 2; i++)
            {
                var hexadecimalCharacter = hexadecimalString.Substring(i * 2, 2);
                var hexadecimalValue = Convert.ToInt32(hexadecimalCharacter, 16);

                builder.Append(char.ConvertFromUtf32(hexadecimalValue));
            }

            return builder.ToString();
        }

        public static X509Certificate2 ToX509Certificate2(this string certificateBase64)
        {
            if (string.IsNullOrEmpty(certificateBase64))
            {
                throw new Exception("Erro na leitura do token. Certifique-se que possui instalado a versão atualizada do assinador de documentos.");
            }

            return new X509Certificate2(Convert.FromBase64String(certificateBase64));
        }

        public static string CombineFilePathWithGuid(this string filePath)
        {
            return Path.Combine(
                Path.GetDirectoryName(filePath),
                Path.GetFileNameWithoutExtension(filePath) + "-" + Guid.NewGuid().ToString("N"),
                Path.GetExtension(filePath)
            );
        }
    }
}
