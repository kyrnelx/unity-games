using System;
using System.Collections.Generic;

namespace Kyrnel.UniKit
{
    public class EventBase { }
    
    public class SignalBus
    {
        private readonly Dictionary<Type, Delegate> _handlers = new();
        private readonly object _lock = new();

        public SignalSubscription Subscribe<T>(Action<T> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            Type key = typeof(T);
            lock (_lock)
            {
                Delegate value = _handlers.GetValueOrDefault(key);
                _handlers[key] = Delegate.Combine(value, handler);
            }

            return new SignalSubscription(this, key, handler);
        }

        public void Unsubscribe<T>(Action<T> handler)
        {
            if (handler == null) return;
            Unsubscribe(typeof(T), handler);
        }

        internal void Unsubscribe(Type key, Delegate handler)
        {
            if(handler == null)
                return;
            lock (_lock)
            {
                if (!_handlers.TryGetValue(key, out var value)) return;
                Delegate removedValue = Delegate.Remove(value, handler);
                if (removedValue == null)
                    _handlers.Remove(key);
                else
                    _handlers[key] = removedValue;   
            }
        }

        public void Fire<T>(T signal)
        {
            Delegate value;
            lock (_lock)
            {
                _handlers.TryGetValue(typeof(T), out value);
            }
            
            if (value == null) return;
            Delegate[] invocationList = value.GetInvocationList();
            foreach (Delegate singleDelegate in invocationList)
            {
                try
                {
                    (singleDelegate as Action<T>)?.Invoke(signal);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        public void Fire<T>() where T : new()
        {
            Fire(new T());
        }
    }
    
    public class SignalSubscription : IDisposable
    {
        private readonly SignalBus _bus;
        private readonly Type      _key;
        private readonly Delegate  _handler;
        private          bool      _disposed;

        internal SignalSubscription(SignalBus bus, Type key, Delegate handler)
        {
            _bus     = bus;
            _key     = key;
            _handler = handler;
        }
        
        public void Dispose()
        {
            if(_disposed) return;
            _bus.Unsubscribe(_key, _handler);
            _disposed = true;
        }
    }
    
    public class SignalSubscriptionCollection : IDisposable
    {
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        private readonly SignalBus         _bus;

        public SignalSubscriptionCollection(SignalBus bus)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        public SignalSubscriptionCollection Subscribe<T>(Action<T> handler)
        {
            _subscriptions.Add(_bus.Subscribe<T>(handler));
            return this;
        }

        public void Dispose()
        {
            foreach (var subscription in _subscriptions)
                subscription.Dispose();
            _subscriptions.Clear();
        }
    }
}