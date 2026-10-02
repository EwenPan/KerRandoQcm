using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace KerRandoQcm.Services;

/// <summary>Resizes uploaded question photos and encodes them as a JPEG data URI for direct MongoDB storage.</summary>
public class ImageProcessingService
{
    private const int MaxDimension = 1200;
    private const int JpegQuality = 80;

    public async Task<string> ResizeToDataUriAsync(Stream input, CancellationToken cancellationToken = default)
    {
        using var image = await Image.LoadAsync(input, cancellationToken);

        image.Mutate(ctx => ctx.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(MaxDimension, MaxDimension)
        }));

        using var output = new MemoryStream();
        await image.SaveAsync(output, new JpegEncoder { Quality = JpegQuality }, cancellationToken);
        return $"data:image/jpeg;base64,{Convert.ToBase64String(output.ToArray())}";
    }
}
