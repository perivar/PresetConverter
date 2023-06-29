
using CommonUtils;
using Serilog;

namespace PresetConverterProject.NIKontaktNKS
{
    /// <summary>
    /// Class to store a NI resource
    /// </summary>
    public class NIResource
    {
        public long Count { get; set; }
        public string Name { get; set; }
        public long Length { get; set; }
        public byte[] Data { get; set; }
        public long EndIndex { get; set; }
    }
}