using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SeaCatCSharpClient.Utils {

    /// <summary>
    /// Executor for all tasks; replaces threading executors (since there is no thread access for WinRT or WinPhone
    /// </summary>
    public static class TaskHelper {

        /// <summary>
        /// Metadata for tasks
        /// </summary>
        class TaskMetadata {
            public Task task;
            public CancellationTokenSource tokenSource;
            public string name;

            public TaskMetadata(Task task, CancellationTokenSource tokenSource, string name) {
                this.task = task;
                this.tokenSource = tokenSource;
                this.name = name;
            }
        }
        
        private static Dictionary<int?, TaskMetadata> _alltasks = new Dictionary<int?, TaskMetadata>();


        /// <summary>
        /// Gets current task or null if it isn't stored in the dictionary
        /// </summary>
        public static Task CurrentTask => GetMetadata(Task.CurrentId)?.task;

        /// <summary>
        /// Checks, if the current task has been interrupted; if so, a TaskCanceledException will be thrown
        /// </summary>
        public static void CheckInterrupt() {
            if (GetMetadata(Task.CurrentId)?.tokenSource.IsCancellationRequested == true) {
                throw new TaskCanceledException("Task has been cancelled");
            }
        }
        
        public static void AbortCurrentTask() {
            GetMetadata(Task.CurrentId)?.tokenSource.Cancel();
        }

        public static void AbortTask(Task task) {
            GetMetadata(Task.CurrentId)?.tokenSource.Cancel();
        }

        /// <summary>
        /// Creates a new task with given name
        /// </summary>
        /// <param name="name">name of the task</param>
        /// <param name="action">action to execute</param>
        /// <returns></returns>
        public static Task CreateTask(string name, Action action) {
            lock (_alltasks) {
                var tokenSource = new CancellationTokenSource();
                var token = tokenSource.Token;

                Logger.Debug(":THREAD:", $"Task {name} created");
                Task newTask = new Task(action, token);
                _alltasks[newTask.Id] = new TaskMetadata(newTask, tokenSource, name);
                // remove task when completed
                newTask.GetAwaiter().OnCompleted(() => _alltasks.Remove(newTask.Id));

                return newTask;
            }
        }

        /// <summary>
        /// Creates a new task of given type with given name
        /// </summary>
        /// <param name="name">name of the task</param>
        /// <param name="action">action to execute</param>
        /// <returns></returns>
        public static Task<T> CreateTask<T>(string name, Func<T> action) {
            lock (_alltasks) {
                var tokenSource = new CancellationTokenSource();
                var token = tokenSource.Token;

                Logger.Debug(":THREAD:", $"Task {name} created");
                Task<T> newTask = new Task<T>(action, token);
                _alltasks[newTask.Id] = new TaskMetadata(newTask, tokenSource, name);
                // remove task when completed
                newTask.GetAwaiter().OnCompleted(() => RemoveTask(newTask.Id));

                return newTask;
            }
        }

        private static TaskMetadata GetMetadata(int? id) {
            if (_alltasks.ContainsKey(id)) {
                return _alltasks[id];
            }

            return null;
        }

        private static void RemoveTask(int? id) {
            var metadata = GetMetadata(id);
            if (metadata != null) {
                _alltasks.Remove(id);
                Logger.Debug(":THREAD:", $"Task {metadata.name} removed");
            }
        }
    }
}
