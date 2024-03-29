using System;
using System.Runtime.InteropServices;

namespace ProtobufLanguageServer.Documents
{
    internal static class FilePathComparer
    {
        private static StringComparer _instance;

        public static StringComparer Instance
        {
            get
            {
                if (_instance == null && RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    _instance = StringComparer.Ordinal;
                }
                else if (_instance == null)
                {
                    _instance = StringComparer.OrdinalIgnoreCase;
                }

                return _instance;
            }
        }
    }
}