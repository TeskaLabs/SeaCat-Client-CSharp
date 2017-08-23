using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace seacat_wp_client.Utils {

    /// <summary>
    /// Event dispatcher that can be used to receive broadcasts from seacat client
    /// </summary>
    public class EventDispatcher {

        private static EventDispatcher _data;
        public static EventDispatcher Dispatcher => _data ?? (_data = new EventDispatcher());

        private readonly List<object> _subscribers = new List<object>();

        private EventDispatcher() {

        }

        /// <summary>
        /// Subscribes for sending messages
        /// </summary>
        /// <param name="subscriber"></param>
        public void Subscribe(object subscriber) {
            if (!_subscribers.Contains(subscriber)) {
                _subscribers.Add(subscriber);
            }
        }

        /// <summary>
        /// Unsubscribes from sending messages
        /// </summary>
        /// <param name="subscriber"></param>
        public void Unsubscribe(object subscriber) {
            if (_subscribers.Contains(subscriber)) {
                _subscribers.Remove(subscriber);
            }
        }

        /// <summary>
        /// Sends broadcast message
        /// </summary>
        /// <param name="message"></param>
        public void SendBroadcast(EventMessage message) {
            foreach (var subscriber in _subscribers) {
                (subscriber as BroadcastReceiver)?.ReceiveMessage(message);
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
