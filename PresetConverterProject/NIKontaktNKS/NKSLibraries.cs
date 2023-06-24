using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

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
              Id = "709", Company = "Gospelmusicians", Name = "Neo-Soul Keys", GenKey = new NksGeneratingKey {
                Key = new byte[] { 0xAB, 0x90, 0x62, 0x62, 0x5F, 0x0C, 0x27, 0x75, 0x2B, 0x5C, 0x2A, 0xC8, 0x19, 0x1A, 0xB3, 0x1E, 0xDA, 0x72, 0x07, 0x42, 0xCB, 0x0B, 0x22, 0xF5, 0xB4, 0x5E, 0xB8, 0x96, 0xB9, 0x9C, 0x0B, 0xD2 },
                IV = new byte[] { 0xA7, 0xFA, 0xC4, 0x1D, 0x35, 0x21, 0x49, 0x59, 0x8D, 0x91, 0xE6, 0x0D, 0xAE, 0xF9, 0x99, 0xDE },
              }
            }
          }
      };
    }

    public class NksLibraryDesc : IEquatable<NksLibraryDesc>
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Company { get; set; }
        public NksGeneratingKey GenKey = new();

        public override string ToString()
        {
            return string.Format("Id: {0}{1}, Name: {2}", Id, string.IsNullOrEmpty(Company) ? "" : ", Company: " + Company, Name);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as NksLibraryDesc);
        }

        public bool Equals(NksLibraryDesc? other)
        {
            return other != null &&
                   Id == other.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }

    public class NksGeneratingKey : IEquatable<NksGeneratingKey>
    {
        public byte[] Key { get; set; }
        public int KeyLength { get { return Key != null ? Key.Length : 0; } }
        public byte[] IV { get; set; }
        public int IVLength { get { return IV != null ? IV.Length : 0; } }

        public override string ToString()
        {
            return string.Format("Key-length: {0}, IV-length: {1}", KeyLength, IVLength);
        }

        public bool Equals(NksGeneratingKey other)
        {
            if (other is null) return false;

            return (this.Key.SequenceEqual(other.Key) && this.IV.SequenceEqual(other.IV));
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as NksGeneratingKey);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Key, IV);
        }

        public static bool operator ==(NksGeneratingKey nksGeneratingKey1, NksGeneratingKey nksGeneratingKey2)
        {
            if (((object)nksGeneratingKey1) == null || ((object)nksGeneratingKey2) == null)
            {
                return Object.Equals(nksGeneratingKey1, nksGeneratingKey2);
            }

            return nksGeneratingKey1.Equals(nksGeneratingKey2);
        }

        public static bool operator !=(NksGeneratingKey nksGeneratingKey1, NksGeneratingKey nksGeneratingKey2)
        {
            if (((object)nksGeneratingKey1) == null || ((object)nksGeneratingKey2) == null)
            {
                return !Object.Equals(nksGeneratingKey1, nksGeneratingKey2);
            }

            return !nksGeneratingKey1.Equals(nksGeneratingKey2);
        }

    }
}