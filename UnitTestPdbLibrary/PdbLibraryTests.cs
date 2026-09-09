using System;
using System.IO;
using NUnit.Framework;
using PdbLibrary;

namespace UnitTestPdbLibrary
{
    [TestFixture]
    public class PdbLibraryTests
    {
        // Fixtures are copied next to the test binary (see the csproj), so resolve
        // them from the test directory rather than a Windows-relative "..\..\" path.
        static string TestData(string name) =>
            Path.Combine(TestContext.CurrentContext.TestDirectory, "testdata", name);

        [Test]
        public void TestPdbFileInvalidPath()
        {
            Assert.Throws<ArgumentException>(() => new PDBFile(new FileInfo(TestData("does-not-exist.pdb"))));
        }

        [Test]
        public void TestPeFileInvalidPath()
        {
            Assert.Throws<ArgumentException>(() => new PEFile(new FileInfo(TestData("does-not-exist.dll"))));
        }

        [Test]
        public void TestPDBFileGuid()
        {
            PDBFile pdbFile = new PDBFile(new FileInfo(TestData("myConsoleCrasher.pdb")));
            string guid = pdbFile.GUID.Value();
            Assert.That(guid, Is.EqualTo("0C0E0F8243B54897952E4DB3E538A2361"));
        }

        [Test]
        public void TestPortablePdbGuid()
        {
            // A Portable PDB (BSJB) — the format a .NET Core / modern SDK app's own
            // assembly PDB ships in. Its {GUID}{age} key must equal the one a dump's
            // CodeView record names, so the backend indexes it into the symsrv store
            // under the same key bugsplat-cdb later looks it up by. This fixture's key
            // is the directory it lives under in bugsplat-cdb's data/symbols-core store.
            PDBFile pdbFile = new PDBFile(new FileInfo(TestData("MyDotNetCrasher.pdb")));
            string guid = pdbFile.GUID.Value();
            Assert.That(guid, Is.EqualTo("0023F679780F4F5D8DE2F203E88AB6721"));
        }

        [Test]
        public void TestPEFileGuid()
        {
            PEFile peFile = new PEFile(new FileInfo(TestData("myConsoleCrasher.exe")));
            string guid = peFile.Guid();
            Assert.That(guid, Is.EqualTo("56BDDA687000"));
        }

        [Test]
        public void TestPEFileGuid2()
        {
            PEFile peFile = new PEFile(new FileInfo(TestData("libgcc_s_sjlj-1.dll")));
            string guid = peFile.Guid();
            Assert.That(guid, Is.EqualTo("000200001E000"));
        }

        [Test, Explicit]
        public void TestSymbolStore()
        {
            // Checks all GUIDs in a given symbol store.
            string dir = @"z:\SymbolServers";
            CheckGuidsInStore(dir, Path.Combine(dir, "pbdtests.log"));
            Assert.Pass();
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
                        string directory = Path.GetFileName(Path.GetDirectoryName(f)).ToUpperInvariant();
                        switch (extension)
                        {
                            case ".pdb":
                                PDBFile pdbFile = new PDBFile(new FileInfo(f));
                                string pdbGuid = pdbFile.GUID.Value();
                                File.AppendAllText(fp, String.Format(" {0} {1}\n", pdbGuid, directory));
                                Assert.That(pdbGuid, Is.EqualTo(directory));
                                break;

                            case ".exe":
                            case ".dll":
                                PEFile peFile = new PEFile(new FileInfo(f));
                                string peGuid = peFile.Guid();
                                File.AppendAllText(fp, String.Format("{0} {1}\n", peGuid, directory));
                                Assert.That(peGuid, Is.EqualTo(directory));
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
