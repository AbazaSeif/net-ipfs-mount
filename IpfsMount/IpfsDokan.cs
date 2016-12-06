﻿using DokanNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.AccessControl;

namespace IpfsMount
{
    /// <summary>
    ///   Maps Dokan opeations into IPFS.
    /// </summary>
    partial class IpfsDokan : IDokanOperations
    {
        const string rootName = @"\";
        static string[] rootFolders = { "ipfs", "ipns" };

        public void Cleanup(string fileName, DokanFileInfo info)
        {
            // Nothing to do.
        }

        public void CloseFile(string fileName, DokanFileInfo info)
        {
            // Nothing to do.
        }

        public NtStatus CreateFile(string fileName, DokanNet.FileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes, DokanFileInfo info)
        {
            Console.WriteLine("CreateFile NYI, {0}, mode {1}", fileName, mode);
            if (mode != FileMode.Open)
                return DokanResult.AccessDenied;

            return DokanResult.Success;
        }

        public NtStatus DeleteDirectory(string fileName, DokanFileInfo info)
        {
            Console.WriteLine("DeleteDirectory NYI");
            throw new NotImplementedException();
        }

        public NtStatus DeleteFile(string fileName, DokanFileInfo info)
        {
            Console.WriteLine("DeleteFile NYI");
            throw new NotImplementedException();
        }

        public NtStatus FindFiles(string fileName, out IList<FileInformation> files, DokanFileInfo info)
        {
            Console.WriteLine("FindFiles, {0}", fileName);

            // The root consists only of the root folders.
            if (fileName == rootName)
            {
                files = rootFolders
                    .Select(name => new FileInformation()
                    {
                        FileName = name,
                        Attributes = FileAttributes.Directory,
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

        public NtStatus FindStreams(string fileName, out IList<FileInformation> streams, DokanFileInfo info)
        {
            Console.WriteLine("FindStreams NYI");
            throw new NotImplementedException();
        }

        public NtStatus FlushFileBuffers(string fileName, DokanFileInfo info)
        {
            Console.WriteLine("FlushFileBuffers NYI");
            throw new NotImplementedException();
        }

        public NtStatus GetDiskFreeSpace(out long freeBytesAvailable, out long totalNumberOfBytes, out long totalNumberOfFreeBytes, DokanFileInfo info)
        {
            Console.WriteLine("GetFreeSpace");
            freeBytesAvailable = 0;
            totalNumberOfBytes = 0;
            totalNumberOfFreeBytes = 0;

            return NtStatus.Success;
        }

        public NtStatus GetFileInformation(string fileName, out FileInformation fileInfo, DokanFileInfo info)
        {
            Console.WriteLine("GetFileInformation {0}", fileName);

            fileInfo = new FileInformation { FileName = fileName };

            // Root info
            if (fileName == rootName)
            {
                fileInfo.Attributes = FileAttributes.Directory;
                fileInfo.LastAccessTime = DateTime.Now;

                return DokanResult.Success;
            }

            // Root folder info
            if (rootFolders.Any(name => fileName == (rootName + name)))
            {
                fileInfo.Attributes = FileAttributes.Directory;
                fileInfo.LastAccessTime = DateTime.Now;

                return DokanResult.Success;
            }

            return DokanResult.NotImplemented;
        }

        public NtStatus GetFileSecurity(string fileName, out FileSystemSecurity security, AccessControlSections sections, DokanFileInfo info)
        {
            Console.WriteLine("GetFileSecurity NYI");
            throw new NotImplementedException();
        }

        public NtStatus GetVolumeInformation(
            out string volumeLabel, 
            out FileSystemFeatures features, 
            out string fileSystemName, 
            DokanFileInfo info)
        {
            Console.WriteLine("Get volumne info");
            volumeLabel = "Ipfs";
            features = FileSystemFeatures.ReadOnlyVolume;
            fileSystemName = "ipfs";
            info.IsDirectory = true;

            return NtStatus.Success;
        }

        public NtStatus LockFile(string fileName, long offset, long length, DokanFileInfo info)
        {
            Console.WriteLine("LockFile NYI");
            return NtStatus.NotImplemented;
        }

        public NtStatus Mounted(DokanFileInfo info)
        {
            Console.WriteLine("Mounted");
            return NtStatus.Success;
        }

        public NtStatus MoveFile(string oldName, string newName, bool replace, DokanFileInfo info)
        {
            Console.WriteLine("MoveFile NYI");
            throw new NotImplementedException();
        }

        public NtStatus ReadFile(string fileName, byte[] buffer, out int bytesRead, long offset, DokanFileInfo info)
        {
            Console.WriteLine("ReadFile NYI");
            throw new NotImplementedException();
        }

        public NtStatus SetAllocationSize(string fileName, long length, DokanFileInfo info)
        {
            Console.WriteLine("SetAllocationSize NYI");
            throw new NotImplementedException();
        }

        public NtStatus SetEndOfFile(string fileName, long length, DokanFileInfo info)
        {
            throw new NotImplementedException();
        }

        public NtStatus SetFileAttributes(string fileName, FileAttributes attributes, DokanFileInfo info)
        {
            throw new NotImplementedException();
        }

        public NtStatus SetFileSecurity(string fileName, FileSystemSecurity security, AccessControlSections sections, DokanFileInfo info)
        {
            throw new NotImplementedException();
        }

        public NtStatus SetFileTime(string fileName, DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime, DokanFileInfo info)
        {
            throw new NotImplementedException();
        }

        public NtStatus UnlockFile(string fileName, long offset, long length, DokanFileInfo info)
        {
            throw new NotImplementedException();
        }

        public NtStatus Unmounted(DokanFileInfo info)
        {
            Console.WriteLine("Unmounted NYI");
            throw new NotImplementedException();
        }

        public NtStatus WriteFile(string fileName, byte[] buffer, out int bytesWritten, long offset, DokanFileInfo info)
        {
            Console.WriteLine("WriteFile NYI");
            throw new NotImplementedException();
        }
    }
}


