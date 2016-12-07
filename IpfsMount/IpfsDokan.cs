﻿using DokanNet;
using Ipfs.Api;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.AccessControl;
using Newtonsoft.Json.Linq;
using System.Security.Principal;

namespace IpfsMount
{
    /// <summary>
    ///   Maps Dokan opeations into IPFS.
    /// </summary>
    partial class IpfsDokan : IDokanOperations
    {
        const string rootName = @"\";
        static string[] rootFolders = { "ipfs", "ipns" };
        static IpfsClient ipfs = new IpfsClient();
        static FileSecurity readonlyFileSecurity = new FileSecurity();
        static DirectorySecurity readonlyDirectorySecurity = new DirectorySecurity();

        static IpfsDokan()
        {
            var everyone = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            var everyoneRead = new FileSystemAccessRule(everyone, 
                FileSystemRights.ReadAndExecute | FileSystemRights.ListDirectory,
                AccessControlType.Allow);
            readonlyFileSecurity.AddAccessRule(everyoneRead);
            readonlyFileSecurity.SetOwner(everyone);
            readonlyFileSecurity.SetGroup(everyone);
            readonlyDirectorySecurity.AddAccessRule(everyoneRead);
            readonlyDirectorySecurity.SetOwner(everyone);
            readonlyDirectorySecurity.SetGroup(everyone);
        }

        public NtStatus CreateFile(string fileName, DokanNet.FileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes, DokanFileInfo info)
        {
            Console.WriteLine("CreateFile NYI, {0}, mode {1}, access {2}", fileName, mode, access);

            // Read only access.
            if (mode != FileMode.Open || (access & DokanNet.FileAccess.WriteData) != 0)
                return DokanResult.AccessDenied;

            // Root and root folders are always present.
            if (fileName == rootName || rootFolders.Any(name => fileName == (rootName + name)))
            {
                info.IsDirectory = true;
                return DokanResult.Success;
            }

            // Get file info from IPFS
            var ipfsFileName = fileName.Replace(@"\", "/");
            try
            {
                var x = ipfs.DoCommand("file/ls", ipfsFileName);
                var r = JObject.Parse(x);
                var hash = (string) r["Arguments"][ipfsFileName];
                var o = (JObject) r["Objects"][hash];
                var file = new IpfsFile()
                {
                    Hash = (string)o["Hash"],
                    Size = (long)o["Size"],
                    IsDirectory = (string)o["Type"] == "Directory",
                    Links = new List<IpfsFileLink>(0)
                };
                var links = o["Links"] as JArray;
                if (links != null)
                {
                    file.Links = links
                        .Select(l => new IpfsFileLink()
                        {
                            Name = (string)l["Name"],
                            Hash = (string)l["Hash"],
                            Size = (long)l["Size"],
                            IsDirectory = (string)l["Type"] == "Directory",
                        })
                        .ToList();
                }
                info.Context = file;
                info.IsDirectory = file.IsDirectory;
            }
            catch
            {
                Console.WriteLine("Not found {0}", fileName);
                return DokanResult.FileNotFound;
            }

            return DokanResult.Success;
        }

        public NtStatus GetFileInformation(string fileName, out FileInformation fileInfo, DokanFileInfo info)
        {
            Console.WriteLine("GetFileInformation {0}", fileName);

            fileInfo = new FileInformation {
                FileName = fileName,
                Attributes = FileAttributes.ReadOnly
            };
            var file = info.Context as IpfsFile;
            if (file != null)
            {
                if (file.IsDirectory)
                    fileInfo.Attributes |= FileAttributes.Directory;
                fileInfo.Length = file.Size;

                return DokanResult.Success;
            }

            // Root info
            if (fileName == rootName)
            {
                fileInfo.Attributes |= FileAttributes.Directory;
                fileInfo.LastAccessTime = DateTime.Now;

                return DokanResult.Success;
            }

            // Root folder info
            if (rootFolders.Any(name => fileName == (rootName + name)))
            {
                fileInfo.Attributes |= FileAttributes.Directory;
                fileInfo.LastAccessTime = DateTime.Now;

                return DokanResult.Success;
            }

            return DokanResult.NotImplemented;
        }

        public NtStatus GetFileSecurity(string fileName, out FileSystemSecurity security, AccessControlSections sections, DokanFileInfo info)
        {
            Console.WriteLine("GetFileSecurity {0}, sections {1}, isdir={2}", fileName, sections, info.IsDirectory);
            security = info.IsDirectory
                ? (FileSystemSecurity) readonlyDirectorySecurity
                : readonlyFileSecurity;
            return DokanResult.Success;
        }

        #region Volumne Operations
        public NtStatus GetDiskFreeSpace(out long freeBytesAvailable, out long totalNumberOfBytes, out long totalNumberOfFreeBytes, DokanFileInfo info)
        {
            Console.WriteLine("GetFreeSpace");
            freeBytesAvailable = 0;
            totalNumberOfBytes = 0;
            totalNumberOfFreeBytes = 0;

            return NtStatus.Success;
        }

        public NtStatus GetVolumeInformation(
            out string volumeLabel,
            out FileSystemFeatures features,
            out string fileSystemName,
            DokanFileInfo info)
        {
            Console.WriteLine("Get volumne info");
            volumeLabel = "InterPlanetary";
            features = FileSystemFeatures.ReadOnlyVolume
                | FileSystemFeatures.CasePreservedNames | FileSystemFeatures.CaseSensitiveSearch 
                | FileSystemFeatures.PersistentAcls | FileSystemFeatures.SupportsRemoteStorage 
                | FileSystemFeatures.UnicodeOnDisk | FileSystemFeatures.SupportsObjectIDs;
            fileSystemName = "ipfs";
            info.IsDirectory = true;

            return NtStatus.Success;
        }

        public NtStatus Mounted(DokanFileInfo info)
        {
            Console.WriteLine("Mounted");
            return NtStatus.Success;
        }

        public NtStatus Unmounted(DokanFileInfo info)
        {
            Console.WriteLine("Unmounted NYI");
            return DokanResult.Success;
        }

        #endregion

        #region File Operations
        public NtStatus ReadFile(string fileName, byte[] buffer, out int bytesRead, long offset, DokanFileInfo info)
        {
            Console.WriteLine("ReadFile NYI, buffer size {0}", buffer.Length);
            var file = (IpfsFile)info.Context;

            // TODO: Not very efficient.  Maybe access the merkle dags.
            using (var data = ipfs.Download("cat", file.Hash))
            {
                // Simulate Seek(offset)
                while (offset > 0)
                {
                    var n = (int)Math.Min(offset, buffer.LongLength);
                    offset -= data.Read(buffer, 0, n);
                }

                // Fill the buffer
                bytesRead = data.Read(buffer, 0, buffer.Length);
                return DokanResult.Success;
            }

            throw new NotImplementedException();
        }

        public NtStatus LockFile(string fileName, long offset, long length, DokanFileInfo info)
        {
            return DokanResult.Success; // LockFile does nothing
        }

        public NtStatus UnlockFile(string fileName, long offset, long length, DokanFileInfo info)
        {
            return DokanResult.Success; // UnlockFile does nothing
        }

        public NtStatus WriteFile(string fileName, byte[] buffer, out int bytesWritten, long offset, DokanFileInfo info)
        {
            bytesWritten = 0;
            return DokanResult.AccessDenied;
        }

        public NtStatus DeleteFile(string fileName, DokanFileInfo info)
        {
            return DokanResult.AccessDenied;
        }

        public NtStatus MoveFile(string oldName, string newName, bool replace, DokanFileInfo info)
        {
            return DokanResult.AccessDenied;
        }

        public NtStatus SetAllocationSize(string fileName, long length, DokanFileInfo info)
        {
            return DokanResult.AccessDenied;
        }

        public NtStatus SetEndOfFile(string fileName, long length, DokanFileInfo info)
        {
            return DokanResult.AccessDenied;
        }

        public NtStatus SetFileAttributes(string fileName, FileAttributes attributes, DokanFileInfo info)
        {
            return DokanResult.AccessDenied;
        }

        public NtStatus SetFileSecurity(string fileName, FileSystemSecurity security, AccessControlSections sections, DokanFileInfo info)
        {
            return DokanResult.AccessDenied;
        }

        public NtStatus SetFileTime(string fileName, DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime, DokanFileInfo info)
        {
            return DokanResult.AccessDenied;
        }
        #endregion

        #region Directory Operations
        public NtStatus FindFiles(string fileName, out IList<FileInformation> files, DokanFileInfo info)
        {
            Console.WriteLine("FindFiles, {0}", fileName);

            var file = info.Context as IpfsFile;
            if (file != null)
            {
                files = file.Links
                    .Select(link => new FileInformation()
                    {
                        FileName = link.Name,
                        Length = link.Size,
                        Attributes = FileAttributes.ReadOnly
                            | (link.IsDirectory ? FileAttributes.Directory : FileAttributes.Normal)
                    })
                    .ToList();
                return DokanResult.Success;
            }

            // The root consists only of the root folders.
            if (fileName == rootName)
            {
                files = rootFolders
                    .Select(name => new FileInformation()
                    {
                        FileName = name,
                        Attributes = FileAttributes.Directory | FileAttributes.ReadOnly,
                        LastAccessTime = DateTime.Now
                    })
                    .ToList();
                return DokanResult.Success;
            }

            // Can not determine the contents of the root folders.
            if (rootFolders.Any(name => fileName == (rootName + name)))
            {
                files = new FileInformation[0];
                return DokanResult.Success;
            }

            files = new FileInformation[0];
            return DokanResult.NotImplemented;
        }

        public NtStatus FindFilesWithPattern(string fileName, string searchPattern, out IList<FileInformation> files, DokanFileInfo info)
        {
            Console.WriteLine("FindFilesWithPattern NYI {0} {1}", fileName, searchPattern);
            files = new FileInformation[0];
            return DokanResult.NotImplemented;
        }

        public NtStatus DeleteDirectory(string fileName, DokanFileInfo info)
        {
            Console.WriteLine("DeleteDirectory NYI");
            return DokanResult.AccessDenied;
        }

        #endregion

        #region Misc Operations
        public void Cleanup(string fileName, DokanFileInfo info)
        {
            // Nothing to do.
        }

        public void CloseFile(string fileName, DokanFileInfo info)
        {
            // Nothing to do.
        }

        public NtStatus FindStreams(string fileName, out IList<FileInformation> streams, DokanFileInfo info)
        {
            streams = null;
            return DokanResult.NotImplemented;
        }

        public NtStatus FlushFileBuffers(string fileName, DokanFileInfo info)
        {
            return DokanResult.Success;
        }
        #endregion

    }
}

