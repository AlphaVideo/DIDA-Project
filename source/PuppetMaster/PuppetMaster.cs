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
        Console.WriteLine("PUPPETMASTER\nReading config from file: " + config_path);

        string customer_path = Path.Combine(base_path, @"Customer\bin\Debug\net6.0\Customer.exe");
        string bank_path = Path.Combine(base_path, @"Bank\bin\Debug\net6.0\Bank.exe");
        string boney_path = Path.Combine(base_path, @"Boney\bin\Debug\net6.0\Boney.exe");

        ProcessStartInfo procInfo;

        //Default startup time: 20 seconds ahead
        string startupTime = DateTime.Now.AddSeconds(20).ToString("s"); //s formatting: yyyy-mm-ddThh:mm:ss

        string[] lines = File.ReadAllLines(config_path);

        //Time Wall Setup
        //T X - Use default value
        //T hh:mm:ss otherwise
        foreach (string command in lines)
        {
            string[] tokens = command.Split(' ');
            if (tokens[0].Equals("T"))
            {
                if (!tokens[1].Equals("X"))
                {
                    //Not using default startup time
                    startupTime = DateTime.Parse(tokens[1]).ToString("s");
                }
                Console.WriteLine("Processes set to start at: " + startupTime);
            }
        }

        foreach (string command in lines)
        {
            string[] tokens = command.Split(' ');

            if (tokens[0].Equals("P"))
            { //New process

                switch (tokens[2])
                {
                    case "client": //Initialized with either "cmd" or "script"
                        procInfo = new ProcessStartInfo(customer_path, tokens[1] + " script " + startupTime);
                        procInfo.UseShellExecute = true;

                        Console.WriteLine("Creating customer subprocess with id " + tokens[1]);
                        Process.Start(procInfo);
                        break;

                    case "bank":
                        procInfo = new ProcessStartInfo(bank_path, tokens[1] + " " + startupTime);
                        procInfo.UseShellExecute = true;

                        Console.WriteLine("Creating bank subprocess with id " + tokens[1]);
                        Process.Start(procInfo);
                        break;

                    case "boney":
                        procInfo = new ProcessStartInfo(boney_path, tokens[1] + " " + startupTime);
                        procInfo.UseShellExecute = true;

                        Console.WriteLine("Creating boney subprocess with id " + tokens[1]);
                        Process.Start(procInfo);
                        break;
                }
            }
        }

        Console.WriteLine("Finished creating subprocesses.");
    }
}