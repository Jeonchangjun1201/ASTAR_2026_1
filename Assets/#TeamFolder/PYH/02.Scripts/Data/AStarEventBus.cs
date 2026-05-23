using System;
using System.Collections.Generic;

namespace _TeamFolder.PYH._02.Scripts.Data
{
    public static class AStarEventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> EventHandlers = new();

        public static void Subscribe<TEvent>(Action<TEvent> handler)
        {
            var eventType = typeof(TEvent);
            if (!EventHandlers.ContainsKey(eventType))
            {
                EventHandlers[eventType] = new List<Delegate>();
            }
            EventHandlers[eventType].Add(handler);
        }
        public static void Unsubscribe<TEvent>(Action<TEvent> handler)
        {
            var eventType = typeof(TEvent);
            if (EventHandlers.TryGetValue(eventType, out var eventHandler))
            {
                eventHandler.Remove(handler);
            }
        }
        public static void Publish<TEvent>(TEvent eventArgs)
        {
            var eventType = typeof(TEvent);
            if (!EventHandlers.TryGetValue(eventType, out var handlers)) return;
            
            foreach (var handler in handlers)
            {
                ((Action<TEvent>)handler)(eventArgs);
            }
        }
    }
}