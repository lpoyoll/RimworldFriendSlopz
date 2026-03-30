using Shared.Misc;
using System;
using System.IO;
using System.Threading;

namespace Shared.Files
{
    public abstract class BaseFile
    {
        private static Semaphore Semaphore { get; set; } = new Semaphore(1, 1);

        public static void Save(string savePath, object obj)
        {
            Semaphore.WaitOne();

            try { Serializer.SerializeToFile(savePath, obj); }
            catch (Exception ex) { Printer.Error(ex); }

            Semaphore.Release();
        }

        public static object Load<T>(string savePath, bool generateIfNull = true)
        {
            Semaphore.WaitOne();

            try
            {
                if (File.Exists(savePath))
                {
                    Semaphore.Release();
                    return Serializer.SerializeFromFile<T>(savePath);
                }

                else
                {
                    if (!generateIfNull)
                    {
                        Semaphore.Release();
                        return null;
                    }

                    else
                    {
                        object file = (T)Activator.CreateInstance(typeof(T));
                        Serializer.SerializeToFile(savePath, file);
                        Semaphore.Release();
                        return file;
                    }
                }
            }
            catch (Exception ex) { Printer.Error(ex); }

            Semaphore.Release();
            return null;
        }
    }
}
