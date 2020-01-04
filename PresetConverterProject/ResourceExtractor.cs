using System;
using System.IO;
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

                var destinationTextFilePath = Path.Combine(destinationDirectoryPath, "strings-resources.txt");
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
                        case "RT_ICON": // Icon 
                            break;
                        case "RT_GROUP_ICON":
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