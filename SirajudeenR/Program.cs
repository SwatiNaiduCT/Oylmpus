//using System.Runtime.CompilerServices;
//using System.Runtime.InteropServices.Marshalling;

using System;

namespace FileAccessor
{
    internal class Program
    {
        static void Main(string[] args)
        {
            using (var application = new FileAccessorApplication())
            {
                application.Run();
            }
        }
    }
}

