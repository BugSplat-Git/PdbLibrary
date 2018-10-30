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
        uint blockSize;
        uint numDirectoryBytes;
        uint[] streamDirectory;
        uint numStreams;

        public Root(FileStream fp)
        {
            this.fp = fp;
            this.ReadData();
        }

        public uint getBlockSize() { return blockSize; }

        public uint numBlocksRequired(uint numBytes, uint blockSize)
        {
            // returns number of blocks that are required to store the specified number of bytes
            return (uint)(Math.Ceiling((float)numBytes / blockSize));
        }

        public Boolean ReadData()
        {
            // LLVM.org refers to the first section of the file as The Superblock
            // At file offset 0 in an MSF file is the MSF SuperBlock, which is laid out as follows:
            //
            // struct SuperBlock {
            //  char FileMagic[sizeof(Magic)];
            //  ulittle32_t BlockSize;
            //  ulittle32_t FreeBlockMapBlock;
            //  ulittle32_t NumBlocks;
            //  ulittle32_t NumDirectoryBytes;
            //  ulittle32_t Unknown;
            //  ulittle32_t BlockMapAddr;
            // };

            uint fileReadOffset = (uint)PdbSignature.signature.Length;
            blockSize = ReadHelper.ReadUInt32(fp, fileReadOffset);

            fileReadOffset = (uint)PdbSignature.signature.Length + 3 * sizeof(int);
            numDirectoryBytes = ReadHelper.ReadUInt32(fp, fileReadOffset);
            uint numBlockMapAddrs = numBlocksRequired(numDirectoryBytes, blockSize);    // number of uints at blockMapAddr
           
            // directory stream address is 5 int pointers after the signature
            fileReadOffset = (uint)PdbSignature.signature.Length + 5 * sizeof(int);
            uint blockMapAddr = ReadHelper.ReadUInt32(fp, fileReadOffset);        
            uint[] blockMapPages = ReadHelper.ReadUInt32Array(fp, blockMapAddr*blockSize, numBlockMapAddrs);

            // read in the bytes identifed by the blockMapPages
            byte[] root_page_data = new byte[numBlockMapAddrs * blockSize];

            for (int i=0; i < numBlockMapAddrs; i++)
            {
                uint root_index = blockMapPages[i];
                fileReadOffset = root_index * this.blockSize;
                fp.Seek(fileReadOffset, SeekOrigin.Begin);
                int n = fp.Read(root_page_data, i * (int)blockSize, (int)blockSize);
                if (n != blockSize)
                {
                    throw new Exception(
                        String.Format("Error reading root pages {0} != {1} at index {2}",
                            n, blockSize, i));
                }
            }

            // Finally, read in the stream directory.  It's a block of uints with the following structure.
            // We store this in a single uint array.
            //
            //    struct StreamDirectory {
            //      ulittle32_t NumStreams;
            //      ulittle32_t StreamSizes[NumStreams];
            //      ulittle32_t StreamBlocks[NumStreams][];
            //    };
            int streamDirectoryIntLength = (int)numDirectoryBytes / sizeof(int);
            streamDirectory = new uint[streamDirectoryIntLength];
            for (int i = 0; i < streamDirectoryIntLength; i++)
            {
                streamDirectory[i] = BitConverter.ToUInt32(root_page_data, i * 4);
            }

            this.numStreams = streamDirectory[0];    // Number of streams is first entry in streamDirectory

            return true;
        }

        uint [] readStreamDirectoryUInts( uint offset, uint num )
        {
            uint [] uints = new uint[num];
            for (int i = 0; i < num; i++ )
            {
                uints[i] = streamDirectory[offset + i];
            }

            return uints;
        }

        public uint stream_size(uint stream_index)
        {
            // get stream's numDirectoryBytes in bytes
            if (stream_index >= numStreams)
            {
                throw new Exception(String.Format(
                    "stream index to large: {0}, {1}", stream_index, numStreams));
            }

            uint size = streamDirectory[1 + stream_index];
            return size;
        }

        public uint[] stream_pages(uint stream_index)
        {
            // get stream's page numbers
            uint blocks_offset = 1 + numStreams;

            for (uint sidx = 0; sidx < stream_index; sidx++)
            {
                uint stream_size = this.stream_size(sidx);
                uint stream_num_pages = this.numBlocksRequired(stream_size, blockSize);
                blocks_offset += stream_num_pages;
            }

            uint num_blocks = numBlocksRequired(this.stream_size(stream_index), this.blockSize);
            //Console.WriteLine(String.Format("num_pages {0}, pages_offset {1}", num_pages, pages_offset));

            uint[] streams = readStreamDirectoryUInts(blocks_offset, num_blocks);
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
            if (BitConverter.IsLittleEndian != true)
            {
                throw new Exception("Invalid architecture.  This code requires little-endian");
            }

            using (FileStream fp = new FileStream(filepath, FileMode.Open, FileAccess.Read))
            {
                // Check signature
                byte[] bytes = new byte[PdbSignature.signature.Length];
                int n = fp.Read(bytes, 0, PdbSignature.signature.Length);

                if (n != PdbSignature.signature.Length)
                {
                    throw new Exception("Invalid PDB signature");
                }

                for (int i = 0; i < PdbSignature.signature.Length; i++)
                {
                    if (PdbSignature.signature[i] != bytes[i])
                    {
                        throw new Exception(
                            String.Format("Invalid PDB signatures differ at {0}. Expected {1} found {2}",
                                            i, PdbSignature.signature[i], bytes[i])
                        );
                    }
                }

                // Create Root stream parser
                Root root = new Root(fp);

                // load the PDB stream page
                uint[] pdb_stream_pages = root.stream_pages(1);

                // load GUID from PDB stream
                int guidBytesLength = 4 * 4 + 2 * 2;
                byte[] guidBytes = new byte[guidBytesLength];
                uint fileReadOffset = pdb_stream_pages[0] * root.getBlockSize();
                fp.Seek(fileReadOffset, SeekOrigin.Begin);
                fp.Read(guidBytes, 0, guidBytesLength);

                uint guid_d1 = BitConverter.ToUInt32(guidBytes, 3 * 4);
                ushort guid_d2 = BitConverter.ToUInt16(guidBytes, 4 * 4);
                ushort guid_d3 = BitConverter.ToUInt16(guidBytes, 4 * 4 + 2);

                byte[] guid_d4 = new byte[8];
                fp.Read(guid_d4, 0, 8);

                // load age from the DBI information
                // (PDB information age changes when using PDBSTR)
                uint[] dbi_stream_pages = root.stream_pages(3);

                if (0 < dbi_stream_pages.Length)
                {
                    fileReadOffset = dbi_stream_pages[0] * root.getBlockSize() + 2 * 4;
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
            if (BitConverter.IsLittleEndian != true)
            {
                throw new Exception("Invalid architecture.  This code requires little-endian");
            }

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
                    throw new Exception("Unable to read PE signature");
                }

                for (int i = 0; i < 4; i++)
                {
                    if (PE_SIGNATURE[i] != peSigBytes[i])
                    {
                        throw new Exception( String.Format("PE signature mismatch index {0}, expected{1}, found{2}",
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
            string rval = String.Format("{0:X8}{1:X}", this.timeDateStamp, this.sizeOfImage);
            return rval;
        }
    }
}

