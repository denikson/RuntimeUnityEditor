using System;
using System.Runtime.InteropServices;

namespace RuntimeUnityEditor.Core.Utils
{
    public static class NativeUtils
    {
        public static NativeLibrary LoadNativeLibrary(string path)
        {
            var lib = LoadLibrary(path);
            return lib == IntPtr.Zero ? null : new NativeLibrary(lib);
        }

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        public class NativeLibrary
        {
            private readonly IntPtr lib;

            public NativeLibrary(IntPtr lib) { this.lib = lib; }

            public T GetFunctionAsDelegate<T>(string name) where T : Delegate
            {
                return Marshal.GetDelegateForFunctionPointer(GetProcAddress(lib, name), typeof(T)) as T;
            }
        }
    }
}