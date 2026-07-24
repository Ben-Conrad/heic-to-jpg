using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using LibHeifSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.Metadata.Profiles.Icc;
using SixLabors.ImageSharp.Metadata.Profiles.Iptc;
using SixLabors.ImageSharp.Metadata.Profiles.Xmp;
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
            ApplyMetadata(image, imageHandle, options);
        }

        var encoder = new JpegEncoder { Quality = Math.Clamp(options.JpegQuality, 1, 100) };
        image.Save(outputPath, encoder);
    }

    // WIC's BitmapMetadata query paths are per-container-format: an object read from
    // a HEIC frame is generally not valid for a JPEG encoder. Reusing it wholesale
    // (as a prior version of this method did) doesn't reliably throw on mismatch -
    // it can silently write no metadata at all. The cross-format "policy" queries
    // below (System.Photo.*, System.GPS.*) are the WIC-supported way to move values
    // between container formats, so we read through those and write a fresh,
    // JPEG-scoped BitmapMetadata instead of transplanting the source object.
    private static readonly string[] WicMetadataPolicyQueries =
    {
        "System.Photo.DateTaken",
        "System.Photo.CameraManufacturer",
        "System.Photo.CameraModel",
        "System.Photo.Orientation",
        "System.Photo.FNumber",
        "System.Photo.ExposureTime",
        "System.Photo.ExposureBias",
        "System.Photo.FocalLength",
        "System.Photo.ISOSpeed",
        "System.Photo.Flash",
        "System.Photo.MeteringMode",
        "System.Photo.LensModel",
        "System.Photo.LensManufacturer",
        "System.Photo.WhiteBalance",
        "System.Photo.FocalLengthInFilm",
        "System.GPS.Latitude",
        "System.GPS.LatitudeRef",
        "System.GPS.Longitude",
        "System.GPS.LongitudeRef",
        "System.GPS.Altitude",
        "System.GPS.AltitudeRef",
        "System.Rating",
        "System.Keywords",
        "System.Title",
        "System.Comment",
        "System.Copyright",
    };

    private static void ConvertWithWic(string sourcePath, string outputPath, ConversionOptions options)
    {
        using var input = File.OpenRead(sourcePath);
        var decoder = BitmapDecoder.Create(input, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var sourceFrame = decoder.Frames[0];

        var jpegEncoder = new JpegBitmapEncoder { QualityLevel = Math.Clamp(options.JpegQuality, 1, 100) };

        BitmapFrame frameToSave;
        if (options.PreserveExif && sourceFrame.Metadata is BitmapMetadata sourceMetadata)
        {
            var jpegMetadata = new BitmapMetadata("jpg");
            var queries = options.PreserveGps
                ? WicMetadataPolicyQueries
                : WicMetadataPolicyQueries.Where(q => !q.StartsWith("System.GPS.", StringComparison.Ordinal));

            foreach (var query in queries)
            {
                object? value;
                try
                {
                    value = sourceMetadata.GetQuery(query);
                }
                catch (Exception)
                {
                    // Source codec doesn't support reading this tag via the policy query; skip it.
                    continue;
                }

                if (value is null)
                {
                    continue;
                }

                try
                {
                    jpegMetadata.SetQuery(query, value);
                }
                catch (Exception)
                {
                    // Target (jpg) codec rejected this tag's type/value; skip it, keep the rest.
                }
            }

            // ColorContexts (ICC profile) are raw bytes, not per-container query paths,
            // so they transfer directly rather than needing the policy-query treatment above.
            try
            {
                frameToSave = BitmapFrame.Create(sourceFrame, sourceFrame.Thumbnail, jpegMetadata, sourceFrame.ColorContexts);
            }
            catch (Exception)
            {
                // Something about the assembled metadata/color context still isn't acceptable
                // to the JPEG encoder as a whole; fall back to pixels-only rather than fail the file.
                frameToSave = BitmapFrame.Create(sourceFrame);
            }
        }
        else
        {
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

    private static void ApplyMetadata(Image image, HeifImageHandle handle, ConversionOptions options)
    {
        ApplyExifMetadata(image, handle, options);
        ApplyXmpMetadata(image, handle);
        ApplyIccProfile(image, handle);
        ApplyIptcMetadata(image, handle);
    }

    private static void ApplyExifMetadata(Image image, HeifImageHandle handle, ConversionOptions options)
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

        // LibHeifSharp's GetExifMetadata() already parses and strips the HEIF Exif
        // item's leading 4-byte TIFF-header-offset field (see HeifImageHandle.cs in
        // libheif-sharp) - these bytes start directly at the TIFF header. An earlier
        // version of this method re-skipped 4 more bytes here, which cut off the
        // byte-order mark and silently produced a 0-tag Exif profile for every file.
        if (bytes is null || bytes.Length == 0)
        {
            return;
        }

        try
        {
            var exifProfile = new ExifProfile(bytes);

            // libheif's default decode already applies any stored rotation/mirroring,
            // so the pixel data is physically upright. Leaving the original Orientation
            // tag in place would cause viewers to rotate an already-correct image again.
            exifProfile.RemoveValue(ExifTag.Orientation);

            if (!options.PreserveGps)
            {
                var gpsTags = exifProfile.Values
                    .Where(v => v.Tag.ToString().StartsWith("Gps", StringComparison.OrdinalIgnoreCase))
                    .Select(v => v.Tag)
                    .ToList();

                foreach (var tag in gpsTags)
                {
                    exifProfile.RemoveValue(tag);
                }
            }

            image.Metadata.ExifProfile = exifProfile;
        }
        catch (ImageProcessingException)
        {
            // Malformed Exif block in the source file; skip metadata rather than fail the conversion.
        }
    }

    private static void ApplyXmpMetadata(Image image, HeifImageHandle handle)
    {
        byte[] bytes;
        try
        {
            bytes = handle.GetXmpMetadata();
        }
        catch (HeifException)
        {
            // No XMP block present in the source file.
            return;
        }

        if (bytes is null || bytes.Length == 0)
        {
            return;
        }

        image.Metadata.XmpProfile = new XmpProfile(bytes);
    }

    private static void ApplyIccProfile(Image image, HeifImageHandle handle)
    {
        HeifIccColorProfile? iccProfile;
        try
        {
            iccProfile = handle.IccColorProfile;
        }
        catch (HeifException)
        {
            return;
        }

        if (iccProfile is null)
        {
            return;
        }

        image.Metadata.IccProfile = new IccProfile(iccProfile.GetIccProfileBytes());
    }

    private static void ApplyIptcMetadata(Image image, HeifImageHandle handle)
    {
        IReadOnlyList<HeifItemId> blockIds;
        try
        {
            blockIds = handle.GetMetadataBlockIds(null!, null!);
        }
        catch (HeifException)
        {
            return;
        }

        foreach (var id in blockIds)
        {
            HeifMetadataBlockInfo info;
            try
            {
                info = handle.GetMetadataBlockInfo(id);
            }
            catch (HeifException)
            {
                continue;
            }

            if (!string.Equals(info.ItemType, "iptc", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                var bytes = handle.GetMetadata(id);
                if (bytes is { Length: > 0 })
                {
                    image.Metadata.IptcProfile = new IptcProfile(bytes);
                }
            }
            catch (HeifException)
            {
                // Malformed IPTC block; skip rather than fail the conversion.
            }
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
