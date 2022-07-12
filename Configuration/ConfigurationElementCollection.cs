using System.Configuration;

namespace Smartgeek.Configuration
{
    public abstract class ConfigurationElementCollection<T, K> : ConfigurationElementCollection
        where T : ConfigurationElement, new()
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new T();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return GetElementKey((T)element);
        }

        protected abstract K GetElementKey(T element);

        public T this[int index]
        {
            get { return (T)base.BaseGet(index); }
            set
            {
                if (base.BaseGet(index) != null)
                {
                    base.BaseRemoveAt(index);
                }
                this.BaseAdd(index, value);
            }
        }

        public T this[K key]
        {
            get { return (T)base.BaseGet(key); }
        }
    }
}
