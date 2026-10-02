using QRCoder;

namespace KerRandoQcm.Services;

public class QrCodeService
{
    /// <summary>
    /// Génère un QR Code sous forme d'URI data:image/png;base64,...
    /// </summary>
    public string GenerateQrCodeDataUri(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeBytes = qrCode.GetGraphic(20);
        return $"data:image/png;base64,{Convert.ToBase64String(qrCodeBytes)}";
    }
}
