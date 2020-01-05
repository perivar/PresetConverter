using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using CommonUtils;
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
                            var icondirectoryResource = resource as IconDirectoryResource;
                            int counter = 0;
                            foreach (var icon in icondirectoryResource.Icons)
                            {
                                // read icon resource header info from the data
                                counter++;
                                var bytes_ = icon.Image.Data;
                                UInt32 width_ = (UInt32)icon.Width;
                                UInt32 height_ = (UInt32)icon.Height;

                                // and save each icon as a independent icon file
                                using (var outputStream = new FileStream(string.Format("{0}_{1}.ICO", destinationFilePath, counter), FileMode.OpenOrCreate))
                                {
                                    using (var iconWriter = new BinaryWriter(outputStream))
                                    {
                                        // 0-1 reserved, 0
                                        iconWriter.Write((byte)0);
                                        iconWriter.Write((byte)0);

                                        // 2-3 image type, 1 = icon, 2 = cursor
                                        iconWriter.Write((short)1);

                                        // 4-5 number of images
                                        iconWriter.Write((short)1);

                                        // image entry 1
                                        // 0 image width
                                        iconWriter.Write((byte)width_);
                                        // 1 image height
                                        iconWriter.Write((byte)height_);

                                        // 2 number of colors
                                        iconWriter.Write((byte)0);

                                        // 3 reserved
                                        iconWriter.Write((byte)0);

                                        // 4-5 color planes
                                        iconWriter.Write((short)0);

                                        // 6-7 bits per pixel
                                        iconWriter.Write((short)32);

                                        // 8-11 size of image data
                                        iconWriter.Write((int)bytes_.Length);

                                        // 12-15 offset of image data
                                        iconWriter.Write((int)(6 + 16));

                                        // write image data
                                        // png data must contain the whole png data file
                                        iconWriter.Write(bytes_);
                                    }
                                }

                            }
                            isAlreadyWritten = true;
                            break;
                        case "RT_ICON": // Icon 

                            var bytes = resource.WriteAndGetBytes();

                            // read icon resource header info from the byte stream
                            UInt32 size = 0;
                            UInt32 width = 0;
                            UInt32 height = 0;
                            UInt16 planes = 0;
                            UInt16 bitCount = 0;
                            UInt32 compression = 0;
                            UInt32 sizeImage = 0;
                            UInt32 XpelsPerMet = 0;
                            UInt32 YpelsPerMet = 0;
                            UInt32 clrUsed = 0;
                            UInt32 clrImportant = 0;
                            using (var inputStream = new MemoryStream(bytes))
                            {
                                using (var iconReader = new BinaryReader(inputStream))
                                {
                                    size = iconReader.ReadUInt32();
                                    width = iconReader.ReadUInt32();
                                    height = iconReader.ReadUInt32();
                                    planes = iconReader.ReadUInt16();
                                    bitCount = iconReader.ReadUInt16();
                                    compression = iconReader.ReadUInt32();
                                    sizeImage = iconReader.ReadUInt32();
                                    XpelsPerMet = iconReader.ReadUInt32();
                                    YpelsPerMet = iconReader.ReadUInt32();
                                    clrUsed = iconReader.ReadUInt32();
                                    clrImportant = iconReader.ReadUInt32();
                                }
                            }

                            // and save as one single icon file
                            using (var outputStream = new FileStream(destinationFilePath + ".ICO", FileMode.OpenOrCreate))
                            {
                                using (var iconWriter = new BinaryWriter(outputStream))
                                {
                                    // 0-1 reserved, 0
                                    iconWriter.Write((byte)0);
                                    iconWriter.Write((byte)0);

                                    // 2-3 image type, 1 = icon, 2 = cursor
                                    iconWriter.Write((short)1);

                                    // 4-5 number of images
                                    iconWriter.Write((short)1);

                                    // image entry 1
                                    // 0 image width
                                    iconWriter.Write((byte)width);
                                    // 1 image height
                                    iconWriter.Write((byte)height);

                                    // 2 number of colors
                                    iconWriter.Write((byte)0);

                                    // 3 reserved
                                    iconWriter.Write((byte)0);

                                    // 4-5 color planes
                                    iconWriter.Write((short)0);

                                    // 6-7 bits per pixel
                                    iconWriter.Write((short)32);

                                    // 8-11 size of image data
                                    iconWriter.Write((int)bytes.Length);

                                    // 12-15 offset of image data
                                    iconWriter.Write((int)(6 + 16));

                                    // write image data
                                    // png data must contain the whole png data file
                                    iconWriter.Write(bytes);
                                }
                            }
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
}