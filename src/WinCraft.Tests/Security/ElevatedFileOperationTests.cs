using System;
using System.IO;
using NUnit.Framework;
using WinCraft.Infrastructure.Ipc;
using WinCraft.Infrastructure.Security;

namespace WinCraft.Tests.Security
{
    [TestFixture]
    internal sealed class ElevatedFileOperationTests
    {
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "WinCraft_FileOps_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }

        [Test]
        public void ExecuteLocal_FileRename_RenamesFile()
        {
            string sourcePath = Path.Combine(_tempDir, "source.txt");
            string destinationPath = Path.Combine(_tempDir, "destination.txt");
            File.WriteAllText(sourcePath, "content");

            var request = new ElevatedCommandRequest
            {
                OperationName = ElevatedOperations.FileRename,
                Payload = DataContractPayloadSerializer.Serialize(
                    new FileOperationRequest { SourcePath = sourcePath, DestinationPath = destinationPath }),
                RequestId = "rename-file"
            };

            var result = ElevatedOperationExecutor.ExecuteLocal(request);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(File.Exists(sourcePath), Is.False);
            Assert.That(File.ReadAllText(destinationPath), Is.EqualTo("content"));
        }

        [Test]
        public void ExecuteLocal_FileDelete_DeletesDirectoryRecursively()
        {
            string directoryPath = Path.Combine(_tempDir, "dir");
            Directory.CreateDirectory(directoryPath);
            File.WriteAllText(Path.Combine(directoryPath, "file.txt"), "content");

            var request = new ElevatedCommandRequest
            {
                OperationName = ElevatedOperations.FileDelete,
                Payload = DataContractPayloadSerializer.Serialize(
                    new FileOperationRequest { SourcePath = directoryPath, Recursive = true }),
                RequestId = "delete-dir"
            };

            var result = ElevatedOperationExecutor.ExecuteLocal(request);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(Directory.Exists(directoryPath), Is.False);
        }

        [Test]
        public void ExecuteLocal_FileSetAttributes_UpdatesAttributes()
        {
            string filePath = Path.Combine(_tempDir, "file.txt");
            File.WriteAllText(filePath, "content");

            var request = new ElevatedCommandRequest
            {
                OperationName = ElevatedOperations.FileSetAttributes,
                Payload = DataContractPayloadSerializer.Serialize(
                    new FileOperationRequest { SourcePath = filePath, Attributes = FileAttributes.Hidden }),
                RequestId = "attributes-file"
            };

            var result = ElevatedOperationExecutor.ExecuteLocal(request);

            Assert.That(result.Succeeded, Is.True);
            Assert.That((File.GetAttributes(filePath) & FileAttributes.Hidden) != 0, Is.True);
        }
    }
}
