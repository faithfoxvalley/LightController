using Google.Protobuf;
using System;
using System.IO;
using Pro.Common.Reflection;

namespace LightController.Pro
{
    public static class ProSerialization
    {
        public static bool TryLoadFile<T>(string fullPath, out T data) where T : IMessage<T>
        {
            try
            {
                IMessage message = (IMessage)typeof(T).Construct();
                using FileStream input = File.OpenRead(fullPath);
                message.MergeFrom(input);
                LogFile.Info($"Opened [{fullPath}]");
                data = (T)message;
                return true;
            }
            catch (Exception e)
            {
                LogFile.Error(e, $"Unable to read data from file [{fullPath}]: ");
                data = default;
                return false;
            }
        }
    }
}
