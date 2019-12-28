using System.Collections.Generic;
using System.Xml.Serialization;

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
    public class Icon
    {
        [XmlElement(ElementName = "Data")]
        public string Data { get; set; }
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
        public Icon Icon { get; set; }
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
}