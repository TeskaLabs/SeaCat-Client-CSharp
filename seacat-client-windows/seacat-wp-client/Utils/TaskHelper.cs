using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace seacat_wp_client.Utils {

    public static class TaskHelper {

        private static Dictionary<int?, Task> _alltasks = new Dictionary<int?, Task>();

        public static Task CurrentTask {
            get { return _alltasks[Task.CurrentId]; }
        }

        public static Task CreateTask(Action action) {
            Task newTask = new Task(action);
            _alltasks[newTask.Id] = newTask;
            return newTask;
        }

    }
}
