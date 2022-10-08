using Common;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

//Simple text file parser to kickstart process initialization
internal class PuppetMaster
{
    static void Main(string[] args)
    {
        //Find Common file directory
        //var currentPath = Directory.GetCurrentDirectory();
        //var parentPath = Path.GetFullPath(Path.Combine(currentPath, @"..\"));
        //var commonPath = Path.GetFullPath(Path.Combine(parentPath, "\\configuration_sample.txt"));

        string[] lines = File.ReadAllLines(@"C:\Users\tomas\source\repos\DAD Project\source\Common\configuration_sample.txt");
        foreach (string command in lines) {
            string[] tokens = command.Split(' ');

            if (tokens[0].Equals("P")) { //New process

                switch (tokens[2])
                {
                    case "client":
                        Process.Start(@"C:\Users\tomas\source\repos\DAD Project\source\Customer\Customer.cs", tokens[1]);
                        break;
                }
            } 
        }
    }
}