using CommonUtils;
using SixLabors.ImageSharp.Formats;
using Vestris.ResourceLib;

namespace PresetConverter
{
    public class ResourceExtractor
    {
        public static void Enumerate(string filename, Func<string, Resource, bool> visit)
        {
            using (var resources = new ResourceInfo())
            {
                resources.Load(filename);

                foreach (var resource in resources)
                {
                    var resourceId = string.Format("{0}/{1}/{2}", resource.Type.TypeName, resource.Name, resource.Language);

                    if (visit(resourceId, resource))
                    {
                        return;
                    }
                }
            }
        }

        public static void List(string filename)
        {
            Enumerate(filename, (resourceId, resource) =>
            {
                Console.WriteLine("{0}\t{1}", resourceId, resource.Size);
                return false;
            });
        }

        public static void Extract(string filename, string id, string destinationFilename)
        {
            Enumerate(filename, (resourceId, resource) =>
            {
                if (resourceId == id)
                {
                    File.WriteAllBytes(destinationFilename, resource.WriteAndGetBytes());
                    return true;
                }
                return false;
            });
        }

        public static void ExtractAll(string filename, string destinationDirectoryPath)
        {
            using (var resources = new ResourceInfo())
            {
                resources.Load(filename);

                var destinationTextFilePath = Path.Combine(destinationDirectoryPath, "RT_STRINGS.TXT");
                File.Delete(destinationTextFilePath); // ensure we are always starting with a clean string file

                foreach (var resource in resources)
                {
                    var resourceId = string.Format("{0}/{1}/{2}", resource.Type.TypeName, resource.Name, resource.Language);
                    var destinationFileName = StringUtils.MakeValidFileName(resourceId);
                    var destinationFilePath = Path.Combine(destinationDirectoryPath, destinationFileName);

                    bool isAlreadyWritten = false;
                    switch (resource.Type.TypeName)
                    {
                        case "RT_BITMAP": // Bitmap 
                            break;
                        case "RT_VERSION": // Version 
                            break;
                        case "RT_GROUP_ICON":

                            var iconGroup = new IconGroup();
                            var icondirectoryResource = resource as IconDirectoryResource;
                            foreach (var icon in icondirectoryResource.Icons)
                            {
                                var iconElement = new IconElement(icon.Image.Data);
                                iconGroup.Icons.Add(iconElement);
                            }

                            iconGroup.Save(destinationFilePath + ".ICO");
                            isAlreadyWritten = true;
                            break;
                        case "RT_ICON": // Icon 

                            var iconSingle = new IconGroup();
                            var bytes = resource.WriteAndGetBytes();
                            var iconElementSingle = new IconElement(bytes);
                            iconSingle.Icons.Add(iconElementSingle);

                            iconSingle.Save(destinationFilePath + ".ICO");
                            isAlreadyWritten = true;
                            break;
                        case "RT_MENU": // Menu 
                            break;
                        case "RT_DIALOG": // Dialog 
                            break;
                        case "RT_STRING": // String 
                            var stringResource = resource as StringResource;
                            foreach (var sRes in stringResource.Strings)
                            {
                                int key = sRes.Key;
                                string value = sRes.Value;

                                File.AppendAllText(destinationTextFilePath, string.Format("{0}: {1}\n", key, value));
                            }
                            isAlreadyWritten = true;
                            break;
                        case "RT_FONT": // Font 
                            break;
                        case "RT_FONTDIR":
                            break;
                        case "RT_ACCELERATOR": // Accelerator 
                            break;
                        case "RT_CURSOR": // Cursor 
                            break;
                        case "RT_GROUP_CURSOR":
                            break;
                        case "RT_MANIFEST": // Manifest
                            break;
                        default:
                            break;
                    }

                    if (!isAlreadyWritten)
                    {
                        var bytes = resource.WriteAndGetBytes();
                        if (bytes != null && bytes.Length > 0)
                        {
                            File.WriteAllBytes(destinationFilePath, bytes);
                        }
                    }
                }
            }
        }
    }

    public class IconGroup
    {
        // image header
        public short Reserved1 { get; set; }
        public short ImageType { get; set; }  // 1 = Icon, 2 = Cursor
        public byte NumberOfColors { get; set; }
        public byte Reserved2 { get; set; }
        public List<IconElement> Icons { get; set; }

        public IconGroup()
        {
            Reserved1 = 0;
            ImageType = 1; // 1 = icon, 2 = cursor
            NumberOfColors = 0;
            Reserved2 = 0;

            Icons = new List<IconElement>();
        }

        public void Save(string filePath)
        {
            using (var outputStream = new FileStream(filePath, FileMode.OpenOrCreate))
            {
                using (var iconWriter = new BinaryWriter(outputStream))
                {
                    // write icon 
                    Write(iconWriter);
                }
            }
        }

        private void Write(BinaryWriter iconWriter)
        {
            // https://www.daubnet.com/en/file-format-ico

            // Ico Group Header (6 bytes)
            iconWriter.Write((short)Reserved1);                 // 0-1   Reserved. Must always be 0.
            iconWriter.Write((short)ImageType);                 // 2-3   Specifies image type: 1 for icon (.ICO) image, 2 for cursor (.CUR) image. Other values are invalid.
            iconWriter.Write((short)Icons.Count);               // 4-5   Specifies number of images in the file.

            int lastIconLength = 0;

            // Header for each entry (16 bytes)
            foreach (var icon in Icons)
            {
                iconWriter.Write((byte)icon.Width);             // 6     Specifies image width in pixels. Can be any number between 0 and 255. Value 0 means image width is 256 pixels.
                iconWriter.Write((byte)icon.Height);            // 7     Specifies image height in pixels. Can be any number between 0 and 255. Value 0 means image height is 256 pixels.
                iconWriter.Write((byte)NumberOfColors);         // 8     Specifies number of colors in the color palette. Should be 0 if the image does not use a color palette.
                iconWriter.Write((byte)Reserved2);              // 9     Reserved. Should be 0.
                iconWriter.Write((short)icon.Planes);           // 10-11 In ICO format: Specifies color planes. Should be 0 or 1.
                iconWriter.Write((short)icon.BitCount);         // 12-13 In ICO format: Specifies bits per pixel.
                iconWriter.Write((uint)icon.Length);            // 14-17 Size of (InfoHeader + ANDbitmap + XORbitmap) in bytes

                // the offset is the first 6 bytes + the 16 header bytes times the number of icon and the last image byte length
                int offsetOfImageData = 6 + (16 * Icons.Count) + lastIconLength;
                iconWriter.Write((uint)offsetOfImageData);      // 18-21 Specifies the offset of BMP or PNG data from the beginning of the ICO/CUR file, where InfoHeader starts

                lastIconLength += icon.Length;
            }

            // InfoHeader (40 bytes + image data)
            foreach (var icon in Icons)
            {
                icon.Write(iconWriter);
            }
        }
    }

    public class IconElement
    {
        // icon entry
        public UInt32 Size { get; set; } // Size of InfoHeader structure = 40
        public UInt32 Width { get; set; }
        public UInt32 Height { get; set; } // Icon Height (added height of XOR-Bitmap and AND-Bitmap)
        public UInt16 Planes { get; set; }
        public UInt16 BitCount { get; set; } // bits per pixel
        public UInt32 Compression { get; set; }
        public UInt32 ImageSize { get; set; }
        public UInt32 XpixelsPerMeter { get; set; }
        public UInt32 YpixelsPerMeter { get; set; }
        public UInt32 ColorsUsed { get; set; }
        public UInt32 ColorsImportant { get; set; }
        public byte[] ImageData { get; set; }
        public byte[] BitMask { get; set; }

        public IImageFormat ImageFormat { get; set; }
        public Image Image { get; set; }

        public int Length
        {
            get
            {
                int length = 0;
                if (Image != null)
                {
                    // PNGs
                    length = ImageData.Length;
                }
                else
                {
                    length = 40 + ImageData.Length + BitMask.Length;
                }

                return length;
            }
        }

        public IconElement(byte[] bytes)
        {
            using (var inputStream = new MemoryStream(bytes))
            {
                using (var iconReader = new BinaryReader(inputStream))
                {
                    Size = iconReader.ReadUInt32();

                    // check if this is really a PNG and not a "old" ICO
                    if (Size != 40)
                    {
                        // this might be a PNG
                        Image = Image.Load(bytes);

                        // save as bytes
                        if (Image != null)
                        {
                            ImageFormat = Image.Metadata.DecodedImageFormat;
                            if (ImageFormat != null)
                            {
                                using (var ms = new MemoryStream())
                                {
                                    Image.Save(ms, ImageFormat);
                                    ImageData = ms.ToArray();
                                    ImageSize = (uint)ImageData.Length;
                                }

                                Width = (uint)Image.Width;
                                Height = (uint)Image.Height;
                                BitCount = (ushort)Image.PixelType.BitsPerPixel;
                            }
                        }

                        Size = 40;
                        BitMask = Array.Empty<byte>();
                        return;
                    }

                    Width = iconReader.ReadUInt32();
                    Height = iconReader.ReadUInt32();
                    Planes = iconReader.ReadUInt16();
                    BitCount = iconReader.ReadUInt16();
                    Compression = iconReader.ReadUInt32();
                    ImageSize = iconReader.ReadUInt32();
                    XpixelsPerMeter = iconReader.ReadUInt32();
                    YpixelsPerMeter = iconReader.ReadUInt32();
                    ColorsUsed = iconReader.ReadUInt32();
                    ColorsImportant = iconReader.ReadUInt32();

                    // something is wrong if ImageSize = 0
                    if (ImageSize == 0)
                    {
                        ImageSize = (uint)(bytes.Length - 40);
                    }

                    ImageData = new byte[ImageSize];
                    iconReader.Read(ImageData, 0, (int)ImageSize);

                    // calculate mask length
                    int maskLength = bytes.Length - 40 - (int)ImageSize;

                    // check if the reading failed
                    if (maskLength < 0) return;

                    BitMask = new byte[maskLength];
                    iconReader.Read(BitMask, 0, (int)maskLength);
                }
            }
        }

        public void Write(BinaryWriter iconWriter)
        {
            if (Image != null)
            {
                // PNGs
                iconWriter.Write(ImageData);
            }
            else
            {
                // original version
                iconWriter.Write(Size);
                iconWriter.Write(Width);
                iconWriter.Write(Height);
                iconWriter.Write(Planes);
                iconWriter.Write(BitCount);
                iconWriter.Write(Compression);
                iconWriter.Write(ImageSize);
                iconWriter.Write(XpixelsPerMeter);
                iconWriter.Write(YpixelsPerMeter);
                iconWriter.Write(ColorsUsed);
                iconWriter.Write(ColorsImportant);

                iconWriter.Write(ImageData);
                iconWriter.Write(BitMask);
                iconWriter.Flush();
            }
        }
    }
}