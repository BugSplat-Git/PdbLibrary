[![BugSplat Banner Image](https://user-images.githubusercontent.com/20464226/149019306-3186103c-5315-4dad-a499-4fd1df408475.png)](https://bugsplat.com)

# BugSplat
### **Crash and error reporting built for busy developers.**

[![Follow @bugsplatco on Twitter](https://img.shields.io/twitter/follow/bugsplatco?label=Follow%20BugSplat&style=social)](https://twitter.com/bugsplatco)
[![Join BugSplat on Discord](https://img.shields.io/discord/664965194799251487?label=Join%20Discord&logo=Discord&style=social)](https://discord.gg/bugsplat)

## 👋 Introduction

PdbLibrary is a utilty for getting GUIDs of Windows binaries and symbol files (`.exe`, `.dll`, `.pdb`) so they can be matched to minidump files and processed by WinDbg, CDB, and/or Visual Studio.

## ⚙️ Installation

PdbLibrary can be installed via NuGet.

```sh
Install-Package PdbLibrary
```

## 🧑‍💻 Usage

Create a new instance of `PDBFile` passing a `FileInfo` that points to a `.pdb` file.

```cs
var pdbFile = new PDBFile(new FileInfo(pdbFilePath));
```

Create a new instance of `PEFile` passing a `FileInfo` that points to a `.exe`, or a `.dll` file.

```cs
var pdbFile = new PDBFile(new FileInfo(peFilePath));
```

The GUID value can be accessed on the instance of the `PDBFile` or `PEFile`.

```cs
var guid = pdbFile.GUID;
```

## 🐛 About

[BugSplat](https://bugsplat.com) is a software crash and error reporting service with support for [Windows C++](https://docs.bugsplat.com/introduction/getting-started/integrations/desktop/cplusplus), [.NET Framework](https://docs.bugsplat.com/introduction/getting-started/integrations/desktop/windows-dot-net-framework), [dotnet](https://docs.bugsplat.com/introduction/getting-started/integrations/cross-platform/dot-net-standard) and [many more](https://docs.bugsplat.com/introduction/getting-started/integrations). BugSplat automatically captures critical diagnostic data such as stack traces, log files, and other runtime information. BugSplat also provides automated incident notifications, a convenient dashboard for monitoring trends and prioritizing engineering efforts, and integrations with popular development tools to maximize productivity and ship more profitable software.