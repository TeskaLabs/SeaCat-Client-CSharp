using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;

namespace SeaCatCSharpClient.Utils {

    /// <summary>
    /// Event dispatcher that can be used to receive broadcasts from seacat client
    /// </summary>
    public class EventDispatcher {

        private static EventDispatcher _data;
        public static EventDispatcher Dispatcher => _data ?? (_data = new EventDispatcher());

        private readonly Dictionary<BroadcastReceiver, CoreDispatcher> _subscribers = new Dictionary<BroadcastReceiver, CoreDispatcher>();

        private EventDispatcher() {

        }

        /// <summary>
        /// Subscribes for sending messages
        /// </summary>
        /// <param name="subscriber"></param>
        public void Subscribe(CoreDispatcher dispatcher, BroadcastReceiver subscriber) {
            if (!_subscribers.ContainsKey(subscriber)) {
                _subscribers.Add(subscriber, dispatcher);
            }
        }

        /// <summary>
        /// Unsubscribes from sending messages
        /// </summary>
        /// <param name="subscriber"></param>
        public void Unsubscribe(BroadcastReceiver subscriber) {
            if (_subscribers.ContainsKey(subscriber)) {
                _subscribers.Remove(subscriber);
            }
        }

        /// <summary>
        /// Sends broadcast message
        /// </summary>
        /// <param name="message"></param>
        public async void SendBroadcast(EventMessage message) {
            foreach (var subscriber in _subscribers)
            {
                // use core dispatcher to invoke the method on UI thread
                await subscriber.Value.RunAsync(CoreDispatcherPriority.Normal, () => subscriber.Key.ReceiveMessage(message));
            }
        }
    }

    public class EventMessage {
        private Dictionary<string, object> extras = new Dictionary<string, object>();

        public EventMessage(string type) {
            this.EventType = type;
        }

        public string EventType { get; set; }

        public void PutExtra(string key, string value) => extras.Add(key, value);

        public string GetString(string key) => extras[key] as string ?? "";

        public void PutExtra(string key, int value) => extras.Add(key, value);

        public int GetInteger(string key) => extras[key] as int? ?? 0;

        public void PutExtra(string key, float value) => extras.Add(key, value);

        public float GetFloat(string key) => extras[key] as float? ?? 0.0f;
    }

    public interface BroadcastReceiver {
        void ReceiveMessage(EventMessage message);
    }
}
