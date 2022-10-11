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
        string base_path = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..\..\"));
        string config_path = Path.Combine(base_path, @"Common\config.txt");
        Console.WriteLine("Reading config from file: " + config_path);

        string customer_path = Path.Combine(base_path, @"Customer\bin\Debug\net6.0\Customer.exe");
        string bank_path = Path.Combine(base_path, @"Bank\bin\Debug\net6.0\Bank.exe");
        string boney_path = Path.Combine(base_path, @"Boney\bin\Debug\net6.0\Boney.exe");


        string[] lines = File.ReadAllLines(config_path);
        foreach (string command in lines)
        {
            string[] tokens = command.Split(' ');

            if (tokens[0].Equals("P"))
            { //New process

                switch (tokens[2])
                {
                    case "client":
                        Console.WriteLine("Creating customer subprocess with id " + tokens[1]);
                        Process.Start(customer_path, tokens[1]);
                        break;
                    case "bank":
                        Console.WriteLine("Creating bank subprocess with id " + tokens[1]);
                        Process.Start(bank_path, tokens[1]);
                        break;
                    case "boney":
                        Console.WriteLine("Creating boney subprocess with id " + tokens[1]);
                        Process.Start(boney_path, tokens[1]);
                        break;
                }
            }
        }

        Console.WriteLine("Finished creating subprocesses.");
    }
}