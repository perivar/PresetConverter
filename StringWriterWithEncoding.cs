using System.IO;
using System.Text;

/// <summary>
/// Class to fix the problem of XmlWriter defaulting to utf-16
/// See http://www.csharp411.com/how-to-force-xmlwriter-or-xmltextwriter-to-use-encoding-other-than-utf-16/
/// <example>
///     StringBuilder sb = new StringBuilder();
///     StringWriterWithEncoding stringWriter = new StringWriterWithEncoding(sb, Encoding.UTF8);
///     XmlWriterSettings settings = new XmlWriterSettings
///     {
///         Indent = true,
///         IndentChars = "\t",
///         NewLineChars = "\r\n",
///         NewLineHandling = NewLineHandling.Replace
///     };
///     using (XmlWriter writer = XmlWriter.Create(stringWriter, settings))
///     {
///         doc.Save(writer);
///     }
/// </example>
/// </summary>
public class StringWriterWithEncoding : StringWriter
{
    public StringWriterWithEncoding(StringBuilder sb, Encoding encoding)
        : base(sb)
    {
        this.m_Encoding = encoding;
    }

    private readonly Encoding m_Encoding;

    public override Encoding Encoding
    {
        get
        {
            return this.m_Encoding;
        }
    }
}