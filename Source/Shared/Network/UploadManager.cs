using System;
using System.IO;

namespace Shared
{
    public class UploadManager
    {
        public FileStream fileStream;

        private FileInfo fileInfo;

        public string filePath;

        public UploadManager(string filePath) { this.filePath = filePath; }

        public void PrepareUpload()
        {
            fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            fileInfo = new FileInfo(filePath);
        }

        public byte[] ReadFile()
        {
            byte[] toReturn = new byte[(int)fileInfo.Length];
            fileStream.Read(toReturn, 0, (int)fileInfo.Length);

            return toReturn;
        }

        public void FinishFileWrite()
        {
            fileStream.Close();
            fileStream.Dispose();
        }
    }
}
