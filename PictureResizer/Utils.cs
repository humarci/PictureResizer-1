using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

public static class Utils
{
    public static bool In<T>(this T subject, IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            if (subject.Equals(item))
                return true;
        }
        return false;
    }

    public static bool EndsWithAny(this string target, IEnumerable<string> suffices)
    {
        foreach (var suffix in suffices)
        {
            if (target.EndsWith(suffix))
                return true;
        }
        return false;
    }

    public static void SaveAsJpeg(this Bitmap bitmap, string outputFilePath, long qualityLevel = 95)
    {
        var encoderParameters = new EncoderParameters(1);
        encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, qualityLevel);
        bitmap.Save(outputFilePath, GetEncoderInfo("image/jpeg"), encoderParameters);
    }

    private static ImageCodecInfo GetEncoderInfo(String mimeType)
    {
        var encoders = ImageCodecInfo.GetImageEncoders();
        for (var i = 0; i < encoders.Length; ++i)
        {
            if (encoders[i].MimeType == mimeType)
                return encoders[i];
        }
        return null;
    }
}
