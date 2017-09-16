using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace prime_num_searcher_gui
{
    class ActiveValidationMessageManager
    {
        private readonly Dictionary<string, List<string>> currentErrors = new Dictionary<string, List<string>> { };
        private LinkedList<Action<string>> onChangedEventList = new LinkedList<Action<string>> { };
        public List<string> this[string propertyName]
        {
            get => (string.IsNullOrEmpty(propertyName) || !currentErrors.ContainsKey(propertyName))
                    ? null
                    : currentErrors[propertyName];
        }
        public bool HasErrors
        {
            get => currentErrors.Count > 0;
        }
        public void Add(string propertyName, string error)
        {
            if (!currentErrors.ContainsKey(propertyName)) currentErrors[propertyName] = new List<string>();
            if (!currentErrors[propertyName].Contains(error))
            {
                currentErrors[propertyName].Add(error);
                this.FireOnChangedEvent(propertyName);
            }
        }
        public void Add(string propertyName, IEnumerable<string> error)
        {
            if (!currentErrors.ContainsKey(propertyName)) currentErrors[propertyName] = new List<string>();
            var add = error.Except(currentErrors[propertyName]);
            currentErrors[propertyName].AddRange(add);
            if (add.Any()) this.FireOnChangedEvent(propertyName);
        }

        public void Remove(string propertyName)
        {
            if (currentErrors.ContainsKey(propertyName))
            {
                currentErrors.Remove(propertyName);
                this.FireOnChangedEvent(propertyName);
            }
        }
        private void FireOnChangedEvent(string propertyName)
        {
            foreach (var e in onChangedEventList) e(propertyName);
        }
        public LinkedListNode<Action<string>> AddOnChangedEventListener(Action<string> listener) => onChangedEventList.AddLast(listener);
        public void RemoveOnChangedEventListener(LinkedListNode<Action<string>> node)
        {
            onChangedEventList.Remove(node);
        }
    }
}
