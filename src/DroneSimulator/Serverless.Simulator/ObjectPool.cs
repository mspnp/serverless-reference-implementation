namespace Serverless.Simulator
{
    using System;
    using System.Collections.Concurrent;

    public class ObjectPool<T>
        where T: class
    {
        private BlockingCollection<ObjectPoolObject> _pool = new BlockingCollection<ObjectPoolObject>();
        private Func<T> _factory;
        private int _poolSize;

        public ObjectPool(Func<T> factory, int poolSize = 10)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _poolSize = poolSize;
            Initialize();
        }

        private void Initialize()
        {
            for (int i = 0; i < _poolSize; i++)
            {
                _pool.Add(new ObjectPoolObject(_factory(), this));
            }
        }

        public ObjectPoolObject GetObject()
        {
            return _pool.Take();
        }

        private void Return(ObjectPoolObject obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            _pool.Add(obj);
        }

        public class ObjectPoolObject : IDisposable
        {
            private T _obj;
            private ObjectPool<T> _objectPool;

            internal ObjectPoolObject(T obj, ObjectPool<T> objectPool)
            {
                _obj = obj ?? throw new ArgumentNullException(nameof(obj));
                _objectPool = objectPool ?? throw new ArgumentNullException(nameof(objectPool));
            }

            public void Dispose()
            {
                _objectPool.Return(this);
            }

            public T Value
            {
                get => _obj;
            }

            public static explicit operator T(ObjectPoolObject poolObject)
            {
                return poolObject._obj;
            }
        }
    }
}