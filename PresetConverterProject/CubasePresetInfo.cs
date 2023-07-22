
namespace PresetConverter
{
    /// <summary>
    /// Simple preset storage object to ensure we only process unique presets
    /// the vstEffectIndex can be different but the rest of the variables must be equal 
    /// </summary>
    public class CubasePresetInfo : IEquatable<CubasePresetInfo>
    {
        public string OutputFileName { get; set; }
        public string PluginName { get; set; }
        public string GUID { get; set; }
        public int VsteffectIndex { get; set; }
        public byte[] Bytes { get; set; }


        public override string ToString()
        {
            return string.Format("{0} {1} {2} {3}", GUID, OutputFileName, PluginName, Bytes.Length);
        }

        public override bool Equals(object obj) => Equals(obj as CubasePresetInfo);
        public override int GetHashCode() => (GUID, OutputFileName, PluginName, Bytes).GetHashCode();

        public bool Equals(CubasePresetInfo other)
        {
            if (other is null) return false;

            return this.OutputFileName.Equals(other.OutputFileName) &&
            this.PluginName.Equals(other.PluginName) &&
            this.GUID.Equals(other.GUID) &&
            this.Bytes.SequenceEqual(other.Bytes);
        }

        public static bool operator ==(CubasePresetInfo presetInfo1, CubasePresetInfo presetInfo2)
        {
            if (((object)presetInfo1) == null || ((object)presetInfo2) == null)
                return Equals(presetInfo1, presetInfo2);

            return presetInfo1.Equals(presetInfo2);
        }

        public static bool operator !=(CubasePresetInfo presetInfo1, CubasePresetInfo presetInfo2)
        {
            if (((object)presetInfo1) == null || ((object)presetInfo2) == null)
                return !Equals(presetInfo1, presetInfo2);

            return !presetInfo1.Equals(presetInfo2);
        }
    }
}