using System;
using System.Collections.Generic;

namespace RelativeNumber
{
    public class ObjectPool<T>
    {
        private readonly Stack<T> _objects;
        private readonly Func<T> _objectGenerator;

        public ObjectPool(Func<T> objectGenerator)
        {
            if (objectGenerator == null) throw new ArgumentNullException(nameof(objectGenerator));

            _objects = new Stack<T>(100);
            _objectGenerator = objectGenerator;
        }

        public T GetObject()
        {
            return _objects.Count == 0 ? _objectGenerator() : _objects.Pop();
        }

        public void PutObject(T item)
        {
            _objects.Push(item);
        }
    }
}
