using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using CommonUtils;
using SixLabors.ImageSharp.Formats;

namespace PresetConverterProject.NIKontaktNKS
{
    // converted the xml using https://xmltocsharp.azurewebsites.net/
    [XmlRoot(ElementName = "NKSEnabled")]
    public class NKSEnabled
    {
        [XmlAttribute(AttributeName = "maxVersion")]
        public string MaxVersion { get; set; }
        [XmlAttribute(AttributeName = "minVersion")]
        public string MinVersion { get; set; }
        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "Application")]
    public class Application
    {
        [XmlAttribute(AttributeName = "maxVersion")]
        public string MaxVersion { get; set; }
        [XmlAttribute(AttributeName = "minVersion")]
        public string MinVersion { get; set; }
        [XmlAttribute(AttributeName = "nativeContent")]
        public string NativeContent { get; set; }
        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "Relevance")]
    public class Relevance
    {
        [XmlElement(ElementName = "Application")]
        public List<Application> Application { get; set; }
        [XmlAttribute(AttributeName = "maxVersion")]
        public string MaxVersion { get; set; }
        [XmlAttribute(AttributeName = "minVersion")]
        public string MinVersion { get; set; }
    }

    [XmlRoot(ElementName = "Visibility")]
    public class Visibility
    {
        [XmlAttribute(AttributeName = "maxVersion")]
        public string MaxVersion { get; set; }
        [XmlAttribute(AttributeName = "minVersion")]
        public string MinVersion { get; set; }
        [XmlAttribute(AttributeName = "target")]
        public string Target { get; set; }
        [XmlText]
        public string Text { get; set; }
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }
    }

    [XmlRoot(ElementName = "Icon")]
    public class IconElement
    {
        // the Data is an icon stored as Base64 String
        // https://codebeautify.org/base64-to-image-converter
        [XmlElement(ElementName = "Data")]
        public string Data
        {
            get
            {
                string base64String = null;
                if (ImageBytes != null)
                {
                    base64String = Convert.ToBase64String(ImageBytes);
                }
                return base64String;
            }
            set
            {
                if (value == null)
                {
                    Image = null;
                    ImageFormat = null;
                }
                else
                {
                    ImageBytes = Convert.FromBase64String(value);
                }
            }
        }

        // store the image bytes in order to conserve the actual bytes
        // otherwise the image encoding and decoding will change the bytes, probably due to other meta-information lost
        [XmlIgnore]
        public byte[] ImageBytes { get; set; }

        [XmlIgnore]
        public IImageFormat ImageFormat { get; set; }

        [XmlIgnore]
        public Image Image { get; set; }

        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "ProductSpecific")]
    public class ProductSpecific
    {
        [XmlElement(ElementName = "HU")]
        public string HU { get; set; }
        [XmlElement(ElementName = "JDX")]
        public string JDX { get; set; }
        [XmlElement(ElementName = "Visibility")]
        public Visibility Visibility { get; set; }
    }

    [XmlRoot(ElementName = "Product")]
    public class Product
    {
        [XmlElement(ElementName = "UPID")]
        public string UPID { get; set; }
        [XmlElement(ElementName = "Name")]
        public string Name { get; set; }
        [XmlElement(ElementName = "Type")]
        public string Type { get; set; }
        [XmlElement(ElementName = "NKSEnabled")]
        public NKSEnabled NKSEnabled { get; set; }
        [XmlElement(ElementName = "Relevance")]
        public List<Relevance> Relevance { get; set; }
        [XmlElement(ElementName = "PoweredBy")]
        public string PoweredBy { get; set; }
        [XmlElement(ElementName = "Visibility")]
        public Visibility Visibility { get; set; }
        [XmlElement(ElementName = "Company")]
        public string Company { get; set; }
        [XmlElement(ElementName = "AuthSystem")]
        public string AuthSystem { get; set; }
        [XmlElement(ElementName = "SNPID")]
        public string SNPID { get; set; }
        [XmlElement(ElementName = "RegKey")]
        public string RegKey { get; set; }
        [XmlElement(ElementName = "Icon")]
        public IconElement Icon { get; set; }
        [XmlElement(ElementName = "ProductSpecific")]
        public ProductSpecific ProductSpecific { get; set; }
        [XmlAttribute(AttributeName = "version")]
        public string Version { get; set; }
    }

    [XmlRoot(ElementName = "ProductHints")]
    public class ProductHints
    {
        [XmlElement(ElementName = "Product")]
        public Product Product { get; set; }
        [XmlAttribute(AttributeName = "spec")]
        public string Spec { get; set; }
    }

    public static class ProductHintsFactory
    {
        public static ProductHints Read(string filePath)
        {
            ProductHints productHints = null;

            // read xml into model
            var serializer = new XmlSerializer(typeof(ProductHints));
            using (var reader = XmlReader.Create(filePath))
            {
                productHints = (ProductHints)serializer.Deserialize(reader);
            }

            return productHints;
        }

        public static ProductHints ReadFromString(string objectData)
        {
            ProductHints productHints = null;

            // read xml into model
            var serializer = new XmlSerializer(typeof(ProductHints));
            using (TextReader reader = new StringReader(objectData))
            {
                productHints = (ProductHints)serializer.Deserialize(reader);
            }

            return productHints;
        }

        public static void Write(ProductHints productHints, string filePath)
        {
            string xmlString = ToString(productHints);

            if (xmlString != null)
            {
                // write to file
                var xmlBytes = Encoding.UTF8.GetBytes(xmlString);
                BinaryFile.ByteArrayToFile(filePath, xmlBytes);
            }
        }

        public static string ToString(ProductHints productHints)
        {
            if (productHints == null) return null;

            // write to xml file
            var serializer = new XmlSerializer(productHints.GetType());
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add("", ""); // don't add xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" 

            StringBuilder sb = new StringBuilder();
            StringWriterWithEncoding stringWriter = new StringWriterWithEncoding(sb, Encoding.UTF8);
            XmlWriterSettings settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true, // when using false, the xml declaration and encoding is added (<?xml version="1.0" encoding="utf-8"?>)
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\n",
                NewLineHandling = NewLineHandling.Replace
            };
            using (XmlWriter writer = XmlWriter.Create(stringWriter, settings))
            {
                // writer.WriteStartDocument(false); // when using OmitXmlDeclaration = false, add the standalone="no" property to the xml declaration

                // write custom xml declaration to duplicate the original xml format
                writer.WriteRaw("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"no\" ?>\r\n");

                serializer.Serialize(writer, productHints, ns);
            }

            // add an extra \n at the end (0A)
            sb.Append("\n");

            // ugly way to remove whitespace in self closing tags when writing xml document
            sb.Replace(" />", "/>");

            string xmlString = sb.ToString();

            // ugly way to add an extra newline after <ProductHints spec="1.0.16"> and before </ProductHints>
            xmlString = Regex.Replace(xmlString, @"(\<ProductHints.*?\>\n)", "$1\n", RegexOptions.IgnoreCase);
            xmlString = Regex.Replace(xmlString, @"(\n\</ProductHints\>)", "\n$1", RegexOptions.IgnoreCase);

            return xmlString;
        }

        /// <summary>
        /// Update the ImageBytes byte array in the ProductHints object using the stored Image and Imageformat
        /// </summary>
        /// <param name="productHints">product hints object</param>
        public static void UpdateImageBytesFromImage(ProductHints productHints)
        {
            if (productHints != null)
            {
                var image = productHints.Product.Icon.Image;
                var imageFormat = productHints.Product.Icon.ImageFormat;
                if (image != null && imageFormat != null)
                {
                    using (var ms = new MemoryStream())
                    {
                        image.Save(ms, imageFormat);

                        // store image bytes
                        productHints.Product.Icon.ImageBytes = ms.ToArray();
                    }
                }
            }
        }

        /// <summary>
        /// Update the Image and Imageformat in the ProductHints object using the stored ImageBytes
        /// </summary>
        /// <param name="productHints">product hints object</param>
        public static void UpdateImageFromImageBytes(ProductHints productHints)
        {
            if (productHints != null)
            {
                var imageBytes = productHints.Product.Icon.ImageBytes;
                if (imageBytes != null)
                {
                    var image = Image.Load(imageBytes);
                    var imageFormat = image.Metadata.DecodedImageFormat;

                    // store format
                    productHints.Product.Icon.ImageFormat = imageFormat;

                    // store image
                    productHints.Product.Icon.Image = image;
                }
            }
        }
    }
}