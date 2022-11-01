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
        int _slotCount;
        PerfectChannel _channel;
        Dictionary<int, bool> _is_frozen;

        public Freezer(int pid, PerfectChannel channel, Timeslots slots)
        {
            _pid = pid;
            _channel = channel;
            _slot_duration = slots.getSlotDuration();
            _slotCount = slots.getMaxSlots();
            _is_frozen = new();

            for (int slot = 1; slot < slots.getMaxSlots(); slot++){
                _is_frozen[slot] = slots.isFrozen(slot, pid);
            }
        }

        public void FreezerCycle(DateTime startTime)
        {
            while (DateTime.Now < startTime) {}

            for (int slot = 1; slot < _slotCount; slot++)
            {
                if (_is_frozen[slot]) {
                    _channel.Freeze();
                } else {
                    _channel.Unfreeze();
                }

                Thread.Sleep(_slot_duration);
            }
        }
    }
}
