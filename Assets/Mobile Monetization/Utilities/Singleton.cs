using System;

public abstract class Singleton<T> where T : class, new()
{
    private static T _instance;
    private static readonly object _lock = new object();

    public static T Instance
    {
        get
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = new T();
                }
                return _instance;
            }
        }
    }

    protected Singleton()
    {
        // Private constructor to prevent direct instantiation
    }

    public void DestroyInstance()
    {
        lock (_lock)
        {
            _instance = null;
        }
    }
}