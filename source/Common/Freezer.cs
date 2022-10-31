using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class Freezer
    {
        int _pid;
        int _slot_duration;
        int _count = 0;
        PerfectChannel _channel;
        InfiniteList<bool> _is_frozen;

        public Freezer(int pid, PerfectChannel channel, Timeslots slots)
        {
            _pid = pid;
            _channel = channel;
            _slot_duration = slots.getSlotDuration();
            _is_frozen = new InfiniteList<bool>(false);

            for (int slot = 0; slot < slots.getMaxSlots(); slot++){
                _is_frozen.Add(slots.isFrozen(slot, pid));
            }
        }

        public void FreezerCycle(DateTime startTime)
        {
            while (DateTime.Now < startTime) {}

            while (true)
            {
                if (_is_frozen.GetItem(_count++)) {
                    channel.Freeze();
                } else {
                    channel.Unfreeze();
                }

                Thread.sleep(_slot_duration);
            }
        }
    }
}
