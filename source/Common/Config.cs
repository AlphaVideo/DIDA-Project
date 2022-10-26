﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Grpc.Core;

namespace Common
{
    public class Config
    {
        //private DateTime startupTime;
        private string config_path;
        int slotDuration = 0;
        int slotCount = 0;
        private Timeslots? timeslots;
        private List<int> boneyIds = new();
        private List<BoneyServerInfo> boneyServers = new();
        private List<BankServerInfo> bankServers = new();
        private Dictionary<int, int> serversPorts = new(); //<ProcessId, Port>

        //Add startupTime to constructor argument if needed
        public Config()
        {
            string base_path = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..\..\"));
            config_path = Path.Combine(base_path, @"Common\config.txt");

            string[] lines = File.ReadAllLines(config_path);
            foreach (string line in lines)
            {
                string[] tokens = line.Split(" ");
                if (tokens.Length == 4 && tokens[0] == "P" && tokens[2] == "boney")
                {
                    boneyIds.Add(int.Parse(tokens[1]));
                    boneyServers.Add(new BoneyServerInfo(tokens[3]));
                    serversPorts.Add(int.Parse(tokens[1]), int.Parse(tokens[3].Split(":")[2]));
                }

                else if (tokens.Length == 4 && tokens[0] == "P" && tokens[2] == "bank")
                    serversPorts.Add(int.Parse(tokens[1]), int.Parse(tokens[3].Split(":")[2]));

                if (tokens.Length == 2 && tokens[0] == "S")
                    slotCount = int.Parse(tokens[1]);

                if (tokens.Length == 2 && tokens[0] == "D")
                {
                    slotDuration = int.Parse(tokens[1]);
                    timeslots = new Timeslots(slotDuration, slotCount); //Duration always comes before allocations, so timeslots can be initialized here
                }

                if (tokens.Length > 1 && tokens[0] == "F")
                {
                    var configSlotId = int.Parse(tokens[1]);
                    var tuples = Regex.Matches(line, @"[(][1-9]\d*,\s(N|F),\s(NS|S)[)]", RegexOptions.None);

                    foreach (var match in tuples)
                    {
                        string tuple = match.ToString();
                        char[] charsToTrim = { '(', ')' };
                        var info = tuple.Trim(charsToTrim);

                        //State = (PID, Frozen?, Suspected?)
                        var state = info.Split(",");
                        var pid = Int32.Parse(state[0]);
                        string frozenState = state[1].Trim();
                        string suspectState = state[2].Trim();

                        if (frozenState == "F")
                            timeslots.addFrozen(configSlotId, pid);
                        if (suspectState == "S")
                            timeslots.addSuspected(configSlotId, pid);
                    }
                }
            }
        }

        public Timeslots getTimeslots()
        {
            return timeslots;
        }

        public int getMyPort(int pid)
        {
            return serversPorts[pid];
        }

        public List<int> getBoneyIds()
        {
            return boneyIds;
        }
        public List<int> getOtherBoneyIds(int myId)
        {
            List<int> ids= new List<int>(boneyIds);
            ids.Remove(myId);
            return ids;
        }

        public List<BoneyServerInfo> getBoneyServerInfos()
        {
            return boneyServers;
        }

        public List<BankServerInfo> getBankServerInfos()
        {
            return bankServers;
        }

        //public DateTime getStartupTime()
        //{ 
        //    return startupTime;
        //}

        public int getSlotDuration()
        {
            return slotDuration;
        }

        public int getSlotCount()
        {
            return slotCount;
        }

    }
}
