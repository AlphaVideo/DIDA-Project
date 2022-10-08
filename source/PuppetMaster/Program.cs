using Common;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

//Simple text file parser to kickstart process initialization
internal class PuppetMaster
{
    static void Main(string[] args)
    {
        //Find Common file directory
        var currentPath = Directory.GetCurrentDirectory();
        var parentPath = Path.GetFullPath(Path.Combine(currentPath, @"..\"));
        var commonPath = Path.GetFullPath(Path.Combine(parentPath, "\\configuration_sample.txt"));

        string[] lines = File.ReadAllLines(commonPath);
        foreach (string line in lines)
            Console.WriteLine(line);

    }
}