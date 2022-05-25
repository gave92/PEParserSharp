using PEParserSharp;
using System;
using System.Linq;
using IO = System.IO;

namespace PEParserTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var imageresDll = IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SystemResources", "imageres.dll.mun");
            if (!IO.File.Exists(imageresDll))
            {
                imageresDll = IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "imageres.dll");
            }

            Console.WriteLine($"Dumping info about: {imageresDll}\n");

            var pe = new PeFile(imageresDll);

            Console.WriteLine(pe.Info);

            Console.WriteLine($"Loading icons from: {imageresDll}\n");

            var icons = pe.ExtractIcons(256, new[] { 3, 35, 109 }); // Folder, Disk, This PC
            
            Console.WriteLine(string.Join(Environment.NewLine, icons.Select(ic => $"Index: {ic.Name}, Format: {ic.IconType}, ByteSize: {ic.IconData.Length}")));

            Console.ReadKey();
        }
    }
}
