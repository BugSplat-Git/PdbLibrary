using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

//
// See https://llvm.org/docs/PDB/DbiStream.html for file format documentation
//
namespace PdbLibrary
{
    public static class PdbSignature
    {
        public static byte[] signature = new byte[] {
            (byte)'M',
            (byte)'i',
            (byte)'c',
            (byte)'r',
            (byte)'o',
            (byte)'s',
            (byte)'o',
            (byte)'f',
            (byte)'t',
            (byte)' ',
            (byte)'C',
            (byte)'/',
            (byte)'C',
            (byte)'+',
            (byte)'+',
            (byte)' ',
            (byte)'M',
            (byte)'S',
            (byte)'F',
            (byte)' ',
            (byte)'7',
            (byte)'.',
            (byte)'0',
            (byte)'0',
            (byte)'\r',
            (byte)'\n',
            (byte)'\x1A',
            (byte)'D',
            (byte)'S',
            (byte)'\x00',
            (byte)'\x00',
            (byte)'\x00',
        };
    }

    //class Program
    //{

    //    static void Main(string[] args)
    //    {
    //        if (args.Count() < 1)
    //        {
    //            Console.WriteLine("Usage: PdbSignature file.pdb");
    //            Environment.Exit(-1);
    //        }

    //        if (BitConverter.IsLittleEndian != true)
    //        {
    //            Console.WriteLine("Big Endian architectures not supported");
    //            Environment.Exit(-2);
    //        }

    //        string ext = Path.GetExtension(args[0]);
    //        if (String.Equals(ext, ".pdb", StringComparison.OrdinalIgnoreCase))
    //        {
    //            PDBFile pdbFile = new PDBFile(args[0]);
    //            Console.WriteLine(String.Format("PDB signature is {0}", pdbFile.guid.Value()));
    //        }
    //        else
    //        {
    //            PEFile peFile = new PEFile(args[0]);
    //            Console.WriteLine(String.Format("PE signature is {0}", peFile.guid()));
    //        }

    //        Environment.Exit(0);
    //    }
    //}

    public static class ReadHelper
    {
        public static int ReadInt32(FileStream fp, uint fileReadOffset)
        {
            int[] intArray = ReadInt32Array(fp, fileReadOffset, 1);
            return intArray[0];
        }

        public static int[] ReadInt32Array(FileStream fp, uint fileReadOffset, uint numIntsToRead)
        {
            byte[] byteArray = new byte[sizeof(int) * numIntsToRead];

            fp.Seek(fileReadOffset, SeekOrigin.Begin);
            int n = fp.Read(byteArray, 0, (int)numIntsToRead * sizeof(int));
            if (n != numIntsToRead * sizeof(int)) return null;

            int[] intArray = new int[numIntsToRead];
            for (int i = 0; i < numIntsToRead; i++)
            {
                intArray[i] = BitConverter.ToInt32(byteArray, i * sizeof(int));
            }

            return intArray;
        }

        public static uint ReadUInt32(FileStream fp, uint fileReadOffset)
        {
            uint[] intArray = ReadUInt32Array(fp, fileReadOffset, 1);
            return intArray[0];
        }

        public static uint[] ReadUInt32Array(FileStream fp, uint fileReadOffset, uint numIntsToRead)
        {
            byte[] byteArray = new byte[sizeof(int) * numIntsToRead];

            fp.Seek(fileReadOffset, SeekOrigin.Begin);
            int n = fp.Read(byteArray, 0, (int)numIntsToRead * sizeof(int));
            if (n != numIntsToRead * sizeof(int)) return null;

            uint[] uintArray = new uint[numIntsToRead];
            for (int i = 0; i < numIntsToRead; i++)
            {
                uintArray[i] = BitConverter.ToUInt32(byteArray, i * sizeof(int));
            }

            return uintArray;
        }
    }

    public class Root
    {
        // A bare bones abstraction of the root stream of an PDB files. Provides
        // methods to read raw chunks of the root stream, as well as accessing
        // data about streams containing streams.

        FileStream fp;
        uint page_size;
        uint size;
        uint[] page_index;

        public Root(FileStream fp, uint page_size, uint size)
        {
            this.fp = fp;
            this.page_size = page_size;
            this.size = size;
            this.page_index = null;
        }

        public uint pages(uint size, uint page_size)
        {
            // calculate number of pages that are required to store the specified number of bytes
            // param size: number of bytes
            // param page_size: page size
            return (uint)(Math.Ceiling((float)size / page_size));
        }

        public Boolean load_pages()
        {
            // get page numbers used by the root stream
            uint num_pages = pages(size, page_size);

            // root stream indexes starts 5 int pointers after the signature
            uint num_root_index_pages = pages(num_pages * sizeof(int), page_size);

            // Something not right here....
            //uint num_root_index_pages = num_pages;

            uint fileReadOffset = (uint)PdbSignature.signature.Length + 5 * sizeof(int);

            // have to read in pages????
            // somthing like... uint[] root_index_pages = readPageUInts(fileReadOffset, num_root_index_pages);
            uint[] root_index_pages = ReadHelper.ReadUInt32Array(fp, fileReadOffset, num_root_index_pages);
            for (int i = 0; i < num_root_index_pages; i++)
            {
                Console.WriteLine(String.Format("root_index_pages[{0}]={1}", i, root_index_pages[i]));
            }

            // read in the root page list
            //root_page_data = b""
            //for root_index in root_index_pages:
            //    self.fp.seek(root_index* self.page_size)
            //    root_page_data += self.fp.read(self.page_size)
            byte[] root_page_data = new byte[num_root_index_pages * page_size];
            for (int i = 0; i < num_root_index_pages; i++)
            {
                uint root_index = root_index_pages[i];
                fileReadOffset = root_index * this.page_size;
                fp.Seek(fileReadOffset, SeekOrigin.Begin);
                int n = fp.Read(root_page_data, i * (int)page_size, (int)page_size);
                if (n != page_size)
                {
                    Console.WriteLine("Error reading root pages");
                    Environment.Exit(-1);
                }
            }

            //page_list_fmt = "<" + ("%dI" % num_pages)
            //return struct.unpack(page_list_fmt, root_page_data[:num_pages * 4])
            page_index = new uint[num_pages];
            for (int i = 0; i < num_pages; i++)
            {
                page_index[i] = BitConverter.ToUInt32(root_page_data, i * 4);
                Console.WriteLine(String.Format("page_index[{0}]={1}", i, page_index[i]));
            }

            return true;
        }

        public uint[] _pages()
        {
            if (this.page_index == null)
            {
                load_pages();
            }
            return this.page_index;
        }


        public uint page_seek(uint page, uint page_offset)
        {
            // move the root stream to a new position
            // :param page: move to this page
            // :param page_offset: move to the offset in the specified page
            uint offset = (_pages()[page]) * page_size + page_offset;
            fp.Seek(offset, SeekOrigin.Begin);

            return offset;
        }

        public uint readPageUInt(uint start)
        {
            byte[] bytes = readPageBytes(start, sizeof(int));
            uint rval = BitConverter.ToUInt32(bytes, 0);
            return rval;
        }

        public uint[] readPageUInts(uint start, int length)
        {
            byte[] bytes = readPageBytes(start, length * sizeof(int));
            uint[] uints = new uint[length];

            for (int i = 0; i < length; i++)
            {
                uints[i] = BitConverter.ToUInt32(bytes, i * sizeof(int));
            }

            return uints;
        }

        public byte[] readPageBytes(uint start, int length)
        {
            // read root stream bytes
            // :param start: the global offset where to read
            // :param length: number of bytes to read
            // :return: root bytes as an bytes array

            uint start_page = start / this.page_size;    // Floor division
            uint start_byte = start % this.page_size;    // Modulo operator

            int partial_size = (int)Math.Min(length, page_size - start_byte);
            byte[] bytes = new byte[length];
            this.page_seek(start_page, start_byte);
            int n = fp.Read(bytes, 0, partial_size);
            int bytesRead = partial_size;
            length -= partial_size;

            while (0 < length)
            {
                Console.WriteLine("Warning: seldom used code block");
                start_page += 1;
                this.page_seek(start_page, 0);
                partial_size = Math.Min((int)page_size, length);
                n = fp.Read(bytes, bytesRead, partial_size);
                bytesRead += partial_size;
                length -= partial_size;
            }

            return bytes;
        }

        public uint num_streams()
        {
            // get number of streams listed in this root stream
            //return struct.unpack("<I", self.read(0, 4))[0]
            uint num_streams = readPageUInt(0);
            return num_streams;
        }


        public uint stream_size(uint stream_index)
        {
            // get stream's size in bytes
            if (stream_index >= this.num_streams())
            {
                Console.Write("stream index to large");
                Environment.Exit(-1);
            }

            // return struct.unpack("<I", self.read(stream_index* 4 + 4, 4))[0]
            uint size = readPageUInt(stream_index * 4 + 4);
            return size;
        }

        public uint[] stream_pages(uint stream_index)
        {
            // get stream's page numbers
            uint num_streams = this.num_streams();
            Console.WriteLine(String.Format("num_streams {0}", num_streams));
            uint pages_offset = 4 + 4 * num_streams;

            for (uint sidx = 0; sidx < stream_index; sidx++)
            {
                uint stream_size = this.stream_size(sidx);
                uint stream_num_pages = this.pages(stream_size, page_size);
                pages_offset += stream_num_pages * 4;
            }

            int num_pages = (int)pages(this.stream_size(stream_index), this.page_size);
            Console.WriteLine(String.Format("num_pages {0}, pages_offset {1}", num_pages, pages_offset));

            // return struct.unpack("<%dI" % num_pages, self.read(pages_offset, 4*num_pages))
            uint[] streams = readPageUInts(pages_offset, num_pages);
            for (int i = 0; i < num_pages; i++)
            {
                Console.WriteLine(String.Format("streams[{0}]={1}", i, streams[i]));
            }

            return streams;
        }
    }

    public class GUID
    {
        //    Represents Globally Unique Identifier (GUID). The value is accessible
        //    via 4 data fields with sizes below:
        //    data1 - 32bit integer
        //    data2 - 16bit integer
        //    data3 - 16bit integer
        //    data4 - 64bit as byte array
        //    age - optional age

        uint data1;
        ushort data2;
        ushort data3;
        byte[] data4;
        uint age;
        bool ageSet = false;

        public GUID(uint data1, ushort data2, ushort data3, byte[] data4, uint age)
        {
            this.data1 = data1;
            this.data2 = data2;
            this.data3 = data3;
            this.data4 = data4;

            this.age = age;
            this.ageSet = true;
        }


        public GUID(uint data1, ushort data2, ushort data3, byte[] data4)
        {
            this.data1 = data1;
            this.data2 = data2;
            this.data3 = data3;
            this.data4 = data4;

            this.ageSet = false;
        }

        public static string ByteArrayToString(byte[] ba)
        {
            return BitConverter.ToString(ba).Replace("-", "");
        }

        public String Value()
        {
            String data4_str = ByteArrayToString(this.data4);
            String rval;

            if (this.ageSet)
            {
                rval = String.Format("{0:X8}{1:X4}{2:X4}{3:X}{4:X}",
                                        this.data1, this.data2, this.data3, data4_str, this.age);
            }
            else
            {
                rval = String.Format("{0:X8}{1:X4}{2:X4}{3:X}",
                                        this.data1, this.data2, this.data3, data4_str);
            }

            return rval;
        }
    }

    public class PDBFile
    {
        // Simple PDB(program database) file parser the loads files GUID and Age fields.
        // The GUID and Age values are accessable as following object members:
        //    guid - PDB file's GUID as an instance of GUID class
        //    age  - PDB file's Age, as an integer
        public GUID guid = null;

        public PDBFile(String filepath)
        {

            using (FileStream fp = new FileStream(filepath, FileMode.Open, FileAccess.Read))
            {
                // Check signature
                byte[] bytes = new byte[PdbSignature.signature.Length];
                int n = fp.Read(bytes, 0, PdbSignature.signature.Length);

                if (n != PdbSignature.signature.Length)
                {
                    Console.WriteLine("Invalid PDB signature");
                    Environment.Exit(-1);
                }

                for (int i = 0; i < PdbSignature.signature.Length; i++)
                {
                    if (PdbSignature.signature[i] != bytes[i])
                    {
                        Console.WriteLine(String.Format("Warning signatures differ at {0}. Expected {1} found {2}", i, PdbSignature.signature[i], bytes[i]));
                    }
                }

                // load page size and root stream definition
                uint fileReadOffset = (uint)PdbSignature.signature.Length;
                uint page_size = ReadHelper.ReadUInt32(fp, fileReadOffset);

                fileReadOffset += 12;
                uint root_dir_size = ReadHelper.ReadUInt32(fp, fileReadOffset);

                // From llvm.org layout notes
                fileReadOffset = (uint)PdbSignature.signature.Length + 3 * sizeof(int);
                uint numDirectoryBytes = ReadHelper.ReadUInt32(fp, fileReadOffset);

                uint numStreamDirectoryBlocks = (uint)Math.Ceiling( (float)numDirectoryBytes / page_size );

                // Create Root stream parser
                Root root = new Root(fp, page_size, root_dir_size);

                // load the PDB stream page
                uint[] pdb_stream_pages = root.stream_pages(1);

                // load GUID from PDB stream
                int guidBytesLength = 4 * 4 + 2 * 2;
                byte[] guidBytes = new byte[guidBytesLength];
                fileReadOffset = pdb_stream_pages[0] * page_size;
                fp.Seek(fileReadOffset, SeekOrigin.Begin);
                fp.Read(guidBytes, 0, guidBytesLength);

                uint guid_d1 = BitConverter.ToUInt32(guidBytes, 3 * 4);
                ushort guid_d2 = BitConverter.ToUInt16(guidBytes, 4 * 4);
                ushort guid_d3 = BitConverter.ToUInt16(guidBytes, 4 * 4 + 2);

                byte[] guid_d4 = new byte[8];
                fp.Read(guid_d4, 0, 8);
                Console.WriteLine(String.Format("guids: {0}, {1}, {2}, {3}", guid_d1, guid_d2, guid_d3, BitConverter.ToString(guid_d4)));

                // load age from the DBI information
                // (PDB information age changes when using PDBSTR)
                uint[] dbi_stream_pages = root.stream_pages(3);

                if (0 < dbi_stream_pages.Length)
                {
                    fileReadOffset = dbi_stream_pages[0] * page_size + 2 * 4;
                    uint age = ReadHelper.ReadUInt32(fp, fileReadOffset);
                    this.guid = new GUID(guid_d1, guid_d2, guid_d3, guid_d4, age);
                }
                else
                {
                    // vc140.pdb however, does not have this stream,
                    // so it does not have an age that can be used
                    // in the hash string
                    this.guid = new GUID(guid_d1, guid_d2, guid_d3, guid_d4);
                }
            }
        }
    }
    public class PEFile
    {

        // Simple PE file parser, that loads two fields used to generate the guid:
        //
        //* TimeDateStamp from file header
        //* SizeOfImage from optional header
        //
        readonly byte[] PE_SIGNATURE = new byte[] { (byte)'P', (byte)'E', 0, 0 };
        const byte PE_SIGNATURE_POINTER = 0x3C;

        const int PE_SIG_SIZE = 4;
        const int MACHINE_SIZE = 2;
        const int NUMBER_OF_SECTION_SIZE = 2;
        const int TIME_DATE_STAMP_SIZE = 4;
        const int POINTER_TO_SYMBOL_TABLE_SIZE = 4;
        const int NUMBER_OF_SYMBOLS_SIZE = 4;
        const int SIZE_OF_OPTIONAL_HEADER_SIZE = 2;
        const int CHARACTERISTICS_SIZE = 2;

        // TimeDateStamp field's offset relative to PE signature
        const int TIME_DATE_STAMP_OFFSET = PE_SIG_SIZE + MACHINE_SIZE + NUMBER_OF_SECTION_SIZE;

        //  Optional header offset relative to PE signature
        const int OPTIONAL_HEADER_OFFSET = PE_SIG_SIZE + MACHINE_SIZE + NUMBER_OF_SECTION_SIZE +
            TIME_DATE_STAMP_SIZE + POINTER_TO_SYMBOL_TABLE_SIZE + NUMBER_OF_SYMBOLS_SIZE +
            SIZE_OF_OPTIONAL_HEADER_SIZE + CHARACTERISTICS_SIZE;

        // SizeOfImage field's offset relative to optional header start
        const int SIZE_OF_IMAGE_OFFSET = 56;

        // Member variables
        int timeDateStamp;
        int sizeOfImage;

        public PEFile(string filepath)
        {
            using (FileStream fp = new FileStream(filepath, FileMode.Open, FileAccess.Read))
            {
                // load PE signature offset
                int pe_sig_offset = ReadHelper.ReadInt32(fp, PE_SIGNATURE_POINTER);

                // check that file contains valid PE signature
                byte[] peSigBytes = new byte[4];
                fp.Seek(pe_sig_offset, SeekOrigin.Begin);
                int n = fp.Read(peSigBytes, 0, 4);
                if (n != 4)
                {
                    Console.WriteLine("Unable to read PE signature");
                    Environment.Exit(-1);
                }

                for (int i = 0; i < 4; i++)
                {
                    if (PE_SIGNATURE[i] != peSigBytes[i])
                    {
                        Console.WriteLine(
                            String.Format("PE signature mismatch index {0}, expected{1}, found{2}",
                                            i, PE_SIGNATURE[i], peSigBytes[i]));
                    }
                }

                // load TimeDateStamp field
                this.timeDateStamp = ReadHelper.ReadInt32(fp, (uint)pe_sig_offset + TIME_DATE_STAMP_OFFSET);

                // load SizeOfImage field
                this.sizeOfImage = ReadHelper.ReadInt32(fp, (uint)pe_sig_offset + OPTIONAL_HEADER_OFFSET + SIZE_OF_IMAGE_OFFSET);
            }
        }

        public string guid()
        {
            string rval = String.Format("{0:X}{1:X}", this.timeDateStamp, this.sizeOfImage);
            return rval;
        }
    }
}

