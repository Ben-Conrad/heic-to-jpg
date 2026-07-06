using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using LibHeifSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;

namespace HeicToJpg.Core;

public sealed class HeicConverter
{
    public string GetOutputPath(string sourcePath, ConversionOptions options)
    {
        var directory = options.OutputLocation == OutputLocationMode.CustomFolder
            && !string.IsNullOrWhiteSpace(options.CustomOutputFolder)
            ? options.CustomOutputFolder!
            : Path.GetDirectoryName(Path.GetFullPath(sourcePath))!;

        var baseName = Path.GetFileNameWithoutExtension(sourcePath);
        var fileName = options.Naming == NamingMode.AddSuffix
            ? $"{baseName}{options.Suffix}.jpg"
            : $"{baseName}.jpg";

        return Path.Combine(directory, fileName);
    }

    public ConversionResult ConvertFile(string sourcePath, ConversionOptions options)
    {
        try
        {
            var outputPath = GetOutputPath(sourcePath, options);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            if (!options.OverwriteExisting && File.Exists(outputPath))
            {
                outputPath = MakeUniquePath(outputPath);
            }

            DecodePath decodePath;
            try
            {
                ConvertWithLibHeif(sourcePath, outputPath, options);
                decodePath = DecodePath.LibHeif;
            }
            catch (HeifException)
            {
                // libheif can reject HEIC variants it doesn't understand yet (e.g. iPhone 15
                // Pro/iOS 18 photos with shared auxiliary images across representations).
                // Fall back to the OS-provided HEIF codec (WIC) for those files.
                ConvertWithWic(sourcePath, outputPath, options);
                decodePath = DecodePath.Wic;
            }

            return new ConversionResult(sourcePath, true, outputPath, null, decodePath);
        }
        catch (Exception ex)
        {
            return new ConversionResult(sourcePath, false, null, ex.Message);
        }
    }

    private static void ConvertWithLibHeif(string sourcePath, string outputPath, ConversionOptions options)
    {
        using var context = new HeifContext(sourcePath);
        using var imageHandle = context.GetPrimaryImageHandle();
        using var decoded = imageHandle.Decode(HeifColorspace.Rgb, HeifChroma.InterleavedRgb24);

        using var image = ToImageSharp(decoded);

        if (options.PreserveExif)
        {
            ApplyExifMetadata(image, imageHandle);
        }

        var encoder = new JpegEncoder { Quality = Math.Clamp(options.JpegQuality, 1, 100) };
        image.Save(outputPath, encoder);
    }

    private static void ConvertWithWic(string sourcePath, string outputPath, ConversionOptions options)
    {
        using var input = File.OpenRead(sourcePath);
        var decoder = BitmapDecoder.Create(input, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var sourceFrame = decoder.Frames[0];

        var jpegEncoder = new JpegBitmapEncoder { QualityLevel = Math.Clamp(options.JpegQuality, 1, 100) };

        BitmapFrame frameToSave;
        try
        {
            frameToSave = options.PreserveExif
                ? BitmapFrame.Create(sourceFrame, sourceFrame.Thumbnail, sourceFrame.Metadata as BitmapMetadata, sourceFrame.ColorContexts)
                : BitmapFrame.Create(sourceFrame);
        }
        catch (NotSupportedException)
        {
            // Some codecs don't support re-attaching metadata; fall back to pixels only.
            frameToSave = BitmapFrame.Create(sourceFrame);
        }

        jpegEncoder.Frames.Add(frameToSave);

        using var output = File.Create(outputPath);
        jpegEncoder.Save(output);
    }

    private static Image<Rgb24> ToImageSharp(HeifImage decoded)
    {
        var width = decoded.Width;
        var height = decoded.Height;
        var plane = decoded.GetPlane(HeifChannel.Interleaved);

        var image = new Image<Rgb24>(width, height);
        var rowBytes = new byte[width * 3];

        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < height; y++)
            {
                var srcRow = plane.Scan0 + (y * plane.Stride);
                Marshal.Copy(srcRow, rowBytes, 0, rowBytes.Length);

                var srcSpan = MemoryMarshal.Cast<byte, Rgb24>(rowBytes);
                srcSpan.CopyTo(accessor.GetRowSpan(y));
            }
        });

        return image;
    }

    private static void ApplyExifMetadata(Image image, HeifImageHandle handle)
    {
        byte[] bytes;
        try
        {
            bytes = handle.GetExifMetadata();
        }
        catch (HeifException)
        {
            // No Exif block present in the source file.
            return;
        }

        // The HEIF "Exif" metadata block starts with a 4-byte big-endian offset
        // to the TIFF header, which precedes the actual Exif payload.
        if (bytes is null || bytes.Length <= 4)
        {
            return;
        }

        try
        {
            var exifProfile = new ExifProfile(bytes[4..]);

            // libheif's default decode already applies any stored rotation/mirroring,
            // so the pixel data is physically upright. Leaving the original Orientation
            // tag in place would cause viewers to rotate an already-correct image again.
            exifProfile.RemoveValue(ExifTag.Orientation);

            image.Metadata.ExifProfile = exifProfile;
        }
        catch (ImageProcessingException)
        {
            // Malformed Exif block in the source file; skip metadata rather than fail the conversion.
        }
    }

    private static string MakeUniquePath(string path)
    {
        var dir = Path.GetDirectoryName(path)!;
        var name = Path.GetFileNameWithoutExtension(path);
        var ext = Path.GetExtension(path);

        var i = 1;
        string candidate;
        do
        {
            candidate = Path.Combine(dir, $"{name} ({i}){ext}");
            i++;
        } while (File.Exists(candidate));

        return candidate;
    }
}
