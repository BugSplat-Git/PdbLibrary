using System;
using System.IO;
using NUnit.Framework;
using PdbLibrary;

namespace UnitTestPdbLibrary
{
    [TestFixture]
    public class PdbLibraryTests
    {
        [Test]
        public void TestPdbFileInvalidPath()
        {
            Assert.Throws<ArgumentException>(() => new PDBFile(new FileInfo(@"..\..\does-not-exist.pdb")));
        }

        [Test]
        public void TestPeFileInvalidPath()
        {
            Assert.Throws<ArgumentException>(() => new PDBFile(new FileInfo(@"..\..\does-not-exist.dll")));
        }

        [Test]
        public void TestPDBFileGuid()
        {
            PDBFile pdbFile = new PDBFile(new FileInfo(@"..\..\testdata\myConsoleCrasher.pdb"));
            string guid = pdbFile.GUID.Value();
            Assert.AreEqual(guid, "0C0E0F8243B54897952E4DB3E538A2361");
        }

        [Test]
        public void TestPEFileGuid()
        {
            PEFile peFile = new PEFile(new FileInfo(@"..\..\testdata\myConsoleCrasher.exe"));
            string guid = peFile.Guid();
            Assert.AreEqual(guid, "56BDDA687000");        
        }

        [Test]
        public void TestPEFileGuid2()
        {
            PEFile peFile = new PEFile(new FileInfo(@"..\..\testdata\libgcc_s_sjlj-1.dll"));
            string guid = peFile.Guid();
            Assert.AreEqual(guid, "000200001E000");        
        }

        [Test]
        public void TestSymbolStore()
        {
            // Checks all GUIDs in a given symbol store.
            string dir = @"z:\SymbolServers";
            //string dir = @"y:\SymbolServers";
            //string dir = @"y:\ossymbols";
            CheckGuidsInStore(dir, dir + @"\pbdtests.log");
            Assert.IsTrue(true);
        }

        void CheckGuidsInStore(string dir, string fp)
        {
            try
            {
                foreach (string d in Directory.GetDirectories(dir))
                {
                    foreach (string f in Directory.GetFiles(d))
                    {
                        File.AppendAllText(fp, String.Format("file: {0}", f));

                        string extension = Path.GetExtension(f);
                        string directory = Path.GetDirectoryName(f);
                        int lastSeparatorIndex = directory.LastIndexOf('\\');
                        directory = directory.Substring(lastSeparatorIndex+1).ToUpper();
                        switch( extension )
                        {
                            case ".pdb":
                                PDBFile pdbFile = new PDBFile(new FileInfo(f));
                                string pdbGuid = pdbFile.GUID.Value();
                                File.AppendAllText(fp, String.Format(" {0} {1}\n", pdbGuid, directory));
                                Assert.AreEqual(pdbGuid, directory);
                                break;

                            case ".exe":
                            case ".dll":
                                PEFile peFile = new PEFile(new FileInfo(f));
                                string peGuid = peFile.Guid();
                                File.AppendAllText(fp, String.Format("{0} {1}\n", peGuid, directory));
                                Assert.AreEqual(peGuid, directory);
                                break;

                            default:
                                File.AppendAllText(fp, "\n");
                                break;
                        }
                    }
                    CheckGuidsInStore(d, fp);
                }
            }
            catch (System.Exception excpt)
            {
                Console.WriteLine(excpt.Message);
            }
        }
    }
}
