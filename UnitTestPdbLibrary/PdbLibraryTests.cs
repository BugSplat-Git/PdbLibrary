using System;
using System.IO;
using PdbLibrary;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTestPdbLibrary
{
    [TestClass]
    public class PdbLibraryTests
    {
        [TestMethod]
        public void TestPDBFileGuid()
        {
            PDBFile pdbFile = new PDBFile(@"..\..\testdata\myConsoleCrasher.pdb");
            string guid = pdbFile.guid.Value();
            Assert.AreEqual(guid, "0C0E0F8243B54897952E4DB3E538A2361");
        }

        [TestMethod]
        public void TestPEFileGuid()
        {
            PEFile peFile = new PEFile(@"..\..\testdata\myConsoleCrasher.exe");
            string guid = peFile.guid();
            Assert.AreEqual(guid, "56BDDA687000");        
        }

        [TestMethod]
        public void TestPEFileGuid2()
        {
            PEFile peFile = new PEFile(@"..\..\testdata\libgcc_s_sjlj-1.dll");
            string guid = peFile.guid();
            Assert.AreEqual(guid, "000200001E000");        
        }

        [TestMethod]
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
                                PDBFile pdbFile = new PDBFile(f);
                                string pdbGuid = pdbFile.guid.Value();
                                File.AppendAllText(fp, String.Format(" {0} {1}\n", pdbGuid, directory));
                                Assert.AreEqual(pdbGuid, directory);
                                break;

                            case ".exe":
                            case ".dll":
                                PEFile peFile = new PEFile(f);
                                string peGuid = peFile.guid();
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
