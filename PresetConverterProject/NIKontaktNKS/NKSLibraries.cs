using System;
using System.Collections.Generic;

namespace PresetConverterProject.NIKontaktNKS
{
    public static class NKSLibraries
    {
        public static Dictionary<String, NksLibraryDesc> Libraries =
        new Dictionary<String, NksLibraryDesc>
        {
          {
          "709",
            new NksLibraryDesc {
              Id = "709", Name = "Neo-Soul Keys", GenKey = new NksGeneratingKey {
                Key = new byte[] { 0xAB, 0x90, 0x62, 0x62, 0x5F, 0x0C, 0x27, 0x75, 0x2B, 0x5C, 0x2A, 0xC8, 0x19, 0x1A, 0xB3, 0x1E, 0xDA, 0x72, 0x07, 0x42, 0xCB, 0x0B, 0x22, 0xF5, 0xB4, 0x5E, 0xB8, 0x96, 0xB9, 0x9C, 0x0B, 0xD2 },
                IV = new byte[] { 0xA7, 0xFA, 0xC4, 0x1D, 0x35, 0x21, 0x49, 0x59, 0x8D, 0x91, 0xE6, 0x0D, 0xAE, 0xF9, 0x99, 0xDE },
              }
            }
          }
      };
    }

    public class NksLibraryDesc
    {
        public String Id { get; set; }
        public string Name { get; set; }
        public NksGeneratingKey GenKey = new NksGeneratingKey();

        public override string ToString()
        {
            return string.Format("Id: {0}, Name: {1}", Id, Name);
        }
    }

    public class NksGeneratingKey
    {
        public byte[] Key { get; set; }
        public int KeyLength { get { return Key != null ? Key.Length : 0; } }
        public byte[] IV { get; set; }
        public int IVLength { get { return IV != null ? IV.Length : 0; } }
    }
}