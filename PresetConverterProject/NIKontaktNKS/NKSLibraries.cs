using System;

namespace PresetConverterProject.NIKontaktNKS
{
    public static class NKSLibraries
    {
        public static NksLibraryDesc[] libraries =
        new NksLibraryDesc[]
        {
          new NksLibraryDesc {
            id = 0x0000000d, name = "Keyboard Collection", gen_key = new NksGeneratingKey {
              key = new byte[] { 0x60 ,0xa2, 0x19, 0x2b}, key_len = 4,
              iv = new byte[] { 0x60, 0xda, 0xb1, 0xcb }, iv_len = 4
            }
          },
          new NksLibraryDesc {
            id = 0x00000065, name = "Stradivari Solo Violin", gen_key = new NksGeneratingKey {
              key = new byte[] { 0x60, 0xe8, 0xdf, 0xe4 }, key_len = 4,
              iv = new byte[] { 0x60, 0x12, 0x23, 0x04 }, iv_len = 4
            }
          },
          new NksLibraryDesc {
            id = 0x00000067, name = "OTTO", gen_key = new NksGeneratingKey {
              key = new byte[] { 0x60, 0x32, 0x29, 0x97 }, key_len = 4,
              iv = new byte[] { 0x60, 0x63, 0x20, 0x37 }, iv_len = 4
            }
          },
          new NksLibraryDesc {
            id = 0x00000068, name = "Acoustic Legends HD", gen_key = new NksGeneratingKey {
              key = new byte[] { 0x60, 0x7c, 0x07, 0xf8 }, key_len = 4,
              iv = new byte[] { 0x60, 0x38, 0x2d, 0x18 }, iv_len = 4
            }
          },
          new NksLibraryDesc {
            id = 0x00000069, name = "Ambience Impacts Rhythms", gen_key = new NksGeneratingKey {
              key = new byte[] { 0x60, 0x86, 0xdf, 0x59 }, key_len = 4,
              iv = new byte[] { 0x60, 0x18, 0xb2, 0xf9 }, iv_len = 4
            }
          },
          new NksLibraryDesc {
            id = 0x0000006a, name = "Chris Hein - Guitars", gen_key = new NksGeneratingKey {
              key = new byte[] { 0x60, 0xa3, 0xa0, 0xaa }, key_len = 4,
              iv = new byte[] { 0x60, 0xa0, 0x3a, 0xca }, iv_len = 4
            }
          },
          new NksLibraryDesc {
            id = 0x0000006b, name = "Solo Strings Advanced", gen_key = new NksGeneratingKey {
              key = new byte[] { 0x60, 0xa3, 0xf3, 0x0a }, key_len = 4,
              iv = new byte[] { 0x60, 0xec, 0xfd, 0x2a }, iv_len = 4
            }
          },
          new NksLibraryDesc {
            id = 0x0000006f, name = "Drums Overkill", gen_key = new NksGeneratingKey {
              key = new byte[] { 0x60, 0x11, 0x14, 0xaa }, key_len = 4,
              iv = new byte[] { 0x60, 0xdf, 0xae, 0xca }, iv_len = 4
            }
          },
          new NksLibraryDesc {
            id = 0x00000073, name = "VI.ONE", gen_key = new NksGeneratingKey {
              key = new byte[] { 0x60, 0x78, 0x29, 0xaf }, key_len = 4,
              iv = new byte[] { 0x60, 0x3a, 0xfc, 0x4f }, iv_len = 4
            }
          },
          new NksLibraryDesc {
            id = 0x00000074, name = "Gofriller Cello", gen_key = new NksGeneratingKey {
              key = new byte[] { 0x60, 0x7d, 0x40, 0xe3 }, key_len = 4,
              iv = new byte[] { 0x60, 0x44, 0x45, 0x83 }, iv_len = 4
            }
          },
          new NksLibraryDesc {
            id = 0x00000195, name = "Evolve Mutations", gen_key = new NksGeneratingKey {
              key = new byte[] { 0x60, 0x4b, 0x2b, 0x29 }, key_len = 4,
              iv = new byte[] { 0x60, 0x47, 0x46, 0xc9 }, iv_len = 4
            }
          },
          new NksLibraryDesc {
            id = 0x00000320, name = "syntAX", gen_key = new NksGeneratingKey {
              key = new byte[] { 0x60, 0x1d, 0xfa, 0x21 }, key_len = 4,
              iv = new byte[] { 0x60, 0x9d, 0xa1, 0xc1 }, iv_len = 4
            }
          },
          new NksLibraryDesc {
            id = 0x00000322, name = "Galaxy II", gen_key = new NksGeneratingKey {
              key = new byte[] { 0x60, 0x2f, 0x74, 0x98 }, key_len = 4,
              iv = new byte[] { 0x60, 0x40, 0xa9, 0xb8 }, iv_len = 4
            }
          },
          new NksLibraryDesc {
            id = 0x00000327, name = "Garritan Instruments for Finale", gen_key = new NksGeneratingKey {
              key = new byte[] { 0x60, 0x55, 0x73, 0xde }, key_len = 4,
              iv = new byte[] { 0x60, 0x3c, 0x3f, 0xfe }, iv_len = 4
            }
          },
          new NksLibraryDesc {
            id = 0x0000033b,  name = "Mr. Sax T", gen_key = new NksGeneratingKey {
              key = new byte[] { 0x60, 0x26, 0x7a, 0x9a }, key_len = 4,
              iv = new byte[] { 0x60, 0x50, 0x2c, 0xba }, iv_len = 4
            }
          },
          new NksLibraryDesc {
            id = 0x0000033c, name = "The Trumpet", gen_key = new NksGeneratingKey {
              key = new byte[] { 0x60, 0xc2, 0x9d, 0xc0 }, key_len = 4,
              iv = new byte[] { 0x60, 0x74, 0x16, 0xe0 }, iv_len = 4
            }
          },
          new NksLibraryDesc {
            id = 0x0000033d, name = "Prominy SC Electric Guitar", gen_key = new NksGeneratingKey {
              key = new byte[] { 0x60, 0x97, 0x26, 0x2b }, key_len = 4,
              iv = new byte[] { 0x60, 0xfc, 0x3e, 0xcb }, iv_len = 4
            }
          },
          new NksLibraryDesc {
            id = 0x00000340, name = "The Elements", gen_key = new NksGeneratingKey {
              key = new byte[] { 0x60, 0xa7, 0x2a, 0x48 }, key_len = 4,
              iv = new byte[] { 0x60, 0x6b, 0xd7, 0x68 }, iv_len = 4
            }
          },
          new NksLibraryDesc {
            id = 0x00000341, name = "Phaedra", gen_key = new NksGeneratingKey {
              key = new byte[] { 0x60, 0xc3, 0x3c, 0x08 }, key_len = 4,
              iv = new byte[] { 0x60, 0xbd, 0x49, 0x28 }, iv_len = 4
            }
          },
          new NksLibraryDesc {
            id = 0x00000343, name = "String Essentials", gen_key = new NksGeneratingKey {
              key = new byte[] { 0x60, 0xf0, 0xf3, 0xfe }, key_len = 4,
              iv = new byte[] { 0x60, 0xaf, 0x90, 0x1e }, iv_len = 4
            }
          },
          new NksLibraryDesc {
            id = 0x00000344, name = "Ethno World 4", gen_key = new NksGeneratingKey {
              key = new byte[] { 0x60, 0xfb, 0xb3, 0xb5 }, key_len = 4,
              iv = new byte[] { 0x60, 0x53, 0xfd, 0x55 }, iv_len = 4
            }
          },
          new NksLibraryDesc {
            id = 0x00000345, name = "Chris Hein Bass", gen_key = new NksGeneratingKey {
              key = new byte[] { 0x60, 0x13, 0x65, 0x88 }, key_len = 4,
              iv = new byte[] { 0x60, 0xef, 0x32, 0xa8 }, iv_len = 4
            }
          },
          new NksLibraryDesc {
            id = 0x00000349, name = "Vir2 Elite Orchestral Percussion", gen_key = new NksGeneratingKey {
              key = new byte[] { 0x60, 0xf1, 0x57, 0x30 }, key_len = 4,
              iv = new byte[] { 0x60, 0xa0, 0xa8, 0x50 }, iv_len = 4
            }
          },
          new NksLibraryDesc {
            id = 0x0000034a, name = "BASiS", gen_key = new NksGeneratingKey {
              key = new byte[] { 0x60, 0x87, 0x67, 0x5d }, key_len = 4,
              iv = new byte[] { 0x60, 0x90, 0x34, 0xfd }, iv_len = 4
            }
          },
          new NksLibraryDesc {
            id = 0x0000034f, name = "Ocean Way Drums Gold", gen_key = new NksGeneratingKey {
              key = new byte[] { 0x60, 0x18, 0xab, 0x90 }, key_len = 4,
              iv = new byte[] { 0x60, 0x11, 0x6c, 0xb0 }, iv_len = 4
            }
          },
          new NksLibraryDesc {
            id = 0x00000351, name = "Evolve", gen_key = new NksGeneratingKey {
              key = new byte[] { 0x60, 0xea, 0x44, 0xd5 }, key_len = 4,
              iv = new byte[] { 0x60, 0x40, 0xde, 0x75 }, iv_len = 4
            }
          },
          new NksLibraryDesc {
            id = 0x00000355, name = "Kreate", gen_key = new NksGeneratingKey {
              key = new byte[] { 0x60, 0x50, 0xe2, 0x5f }, key_len = 4,
              iv = new byte[] { 0x60, 0x82, 0xac, 0xff }, iv_len = 4
            }
          },
          new NksLibraryDesc {
            id = 0x00000356, name = "Symphobia", gen_key = new NksGeneratingKey {
              key = new byte[] { 0x60, 0x5f, 0x52, 0xc5 }, key_len = 4,
              iv = new byte[] { 0x60, 0x95, 0x04, 0x65 }, iv_len = 4
            }
          },
          new NksLibraryDesc {
            id = 0x0000035e, name = "Ocean Way Drums Expandable", gen_key = new NksGeneratingKey {
              key = new byte[] { 0x60, 0x2d, 0x19, 0x0e }, key_len = 4,
              iv = new byte[] { 0x60, 0x80, 0x1d, 0x2e }, iv_len = 4
            }
          },
          new NksLibraryDesc {
            id = 0x00000360, name = "Steven Slate Drums Platinum", gen_key = new NksGeneratingKey {
              key = new byte[] { 0x60, 0x67, 0x52, 0x8e }, key_len = 4,
              iv = new byte[] { 0x60, 0x84, 0x16, 0xae }, iv_len = 4
            }
          },
          new NksLibraryDesc {
            id = 0x0000038a, name = "Chris Hein Horns Vol 2", gen_key = new NksGeneratingKey {
              key = new byte[] { 0x60, 0xc8, 0xf7, 0xa9 }, key_len = 4,
              iv = new byte[] { 0x60, 0x72, 0x53, 0x49 }, iv_len = 4
            }
          },
          new NksLibraryDesc {
            id = 0x00001388, name = "UserPatches", gen_key = new NksGeneratingKey {
              key = new byte[] { 0x60, 0xd0, 0xde, 0x83 }, key_len = 4,
              iv = new byte[] { 0x60, 0x63, 0x73, 0x23 }, iv_len = 4
            }
          },
      };
    }

    public class NksLibraryDesc
    {
        public UInt32 id;
        public string name;
        public NksGeneratingKey gen_key = new NksGeneratingKey();
    }

    public class NksGeneratingKey
    {
        public byte[] key = new byte[32];
        public byte key_len;
        public byte[] iv = new byte[16];
        public byte iv_len;
    }
}