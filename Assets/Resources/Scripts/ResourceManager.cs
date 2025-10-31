using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Resources.Scripts
{
    public class ResourceManager : MonoBehaviour
    {
        private Dictionary<string,int> _collected = new Dictionary<string,int>();

        public void AddCollected(string resourceName, int amount)
        {
            _collected.TryAdd(resourceName, 0);
            _collected[resourceName] += amount;
        }

        public int GetCollected(string resourceName)
        {
            return _collected.GetValueOrDefault(resourceName, 0);
        }

        public int GetTotalCollected()
        {
            return _collected.Count == 0 ? 0 : _collected.Values.Sum();
        }

        public Dictionary<string,int> GetAllCollected()
        {
            return new Dictionary<string,int>(_collected);
        }

        public void SetAllCollected(Dictionary<string,int> data)
        {
            _collected = data != null ? new Dictionary<string,int>(data) : new Dictionary<string,int>();
        }
    }
}