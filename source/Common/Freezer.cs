using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class Freezer
    {
        private PerfectChannel _channel;
        private Timeslots _slots;
        private int _count = 0;

        public Freezer(PerfectChannel channel, Timeslots slots)
        {
            _channel = channel;
            _slots = slots;
        }

        public void FreezerCycle(DateTime startTime)
        {

            while (DateTime.Now < startTime)
            {

            }

            while (true)
            {

            }
        }
    }
}
