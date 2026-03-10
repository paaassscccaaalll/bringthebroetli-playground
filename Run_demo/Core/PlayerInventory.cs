using System.Collections.Generic;

namespace BringTheBrotliDemo
{
    public class PlayerInventory
    {
        private readonly Dictionary<ResourceType, int> _carried = new();

        public int Get(ResourceType type) =>
            _carried.TryGetValue(type, out int amount) ? amount : 0;

        public void Set(ResourceType type, int amount) =>
            _carried[type] = amount;

        public void Add(ResourceType type, int delta, int max) =>
            _carried[type] = System.Math.Min(Get(type) + delta, max);

        public void Reset() => _carried.Clear();
    }
}
