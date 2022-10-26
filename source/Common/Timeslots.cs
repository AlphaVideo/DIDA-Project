using System.Xml.Linq;
using Grpc.Net.Client;

namespace Common
{
    public class Slot
    {
        int _slotId; //Maybe redundant but I'll leave it be for now
        Nullable<int> _value = null;
        List<int> _frozenIds = new();
        List<int> _suspectedIds = new();

        public Slot(int slotId)
        {
            _slotId = slotId;
        }

        public bool isFrozen(int pid)
        {
            return _frozenIds.Contains(pid);
        }

        public void addFrozen(int pid)
        {
            _frozenIds.Add(pid);
        }

        public bool isSuspected(int pid)
        {
            return _suspectedIds.Contains(pid);
        }

        public void addSuspect(int pid)
        {
            _suspectedIds.Add(pid);
        }

        public void swapValue(int val)
        {
            _value = val;
        }

        public List<int> getSuspectList()
        {
            return _suspectedIds;
        }
    }

    public class Timeslots
    {
        int _slotDuration;
        int _maxSlots;
        Dictionary<int, Slot> _slots = new();


        public Timeslots(int duration, int slots)
        {
            _slotDuration = duration;
            _maxSlots = slots;

            for(int i = 1; i <= _maxSlots; i++)
            {
                _slots.Add(i, new Slot(i));
            }
        }

        public bool isSuspected(int slotId, int pid)
        {
            return _slots[slotId].isSuspected(pid);
        }

        public void addSuspected(int slotId, int pid)
        {
            _slots[slotId].addSuspect(pid);
        }

        public bool isFrozen(int slotId, int pid)
        {
            return _slots[slotId].isFrozen(pid);
        }

        public void addFrozen(int slotId, int pid)
        {
            _slots[slotId].addFrozen(pid);
        }

        public void swapValue(int slotId, int val)
        {
            _slots[slotId].swapValue(val);
        }

        public int getSlotDuration()
        {
            return _slotDuration;
        }

        public int getMaxSlots()
        {
            return _maxSlots;
        }

        public List<int> getMySuspectList(int slotId, int myId)
        {
            var suspectList = new List<int>(_slots[slotId].getSuspectList());
            suspectList.Remove(myId);
            return suspectList;
        }
      
    }
}