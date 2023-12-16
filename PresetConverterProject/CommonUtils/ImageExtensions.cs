
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;

namespace CommonUtils
{
    public static class ImageExtensions
    {
        public static byte[] ToByteArray(this Image image, IImageFormat format)
        {
            using (var memoryStream = new MemoryStream())
            {
                image.Save(memoryStream, format);
                return memoryStream.ToArray();
            }
        }

        public static void Save(this Image image, string path, IImageFormat? imageFormat)
        {
            // this defaults to PNG if the format is not passed.
            // you can get the image format using e.g.
            // IImageFormat? imageFormat = image.Metadata.DecodedImageFormat;

            if (imageFormat != null)
            {
                IImageEncoder encoder = image.Configuration.ImageFormatsManager.GetEncoder(imageFormat);
                image.Save(path, encoder);
            }
            else
            {
                // default to PNG
                image.SaveAsPng(path);
            }
        }
    }
}