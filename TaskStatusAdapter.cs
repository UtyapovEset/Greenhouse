using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Greenhose
{
    internal class TaskStatusAdapter : ITaskStatusAdapter
    {
        private readonly WorkTasks _task;

        public TaskStatusAdapter(WorkTasks task)
        {
            _task = task;
        }

        public void SetPlanned()
        {
            _task.Status = "Запланирована";
        }

        public void SetInProgress()
        {
            _task.Status = "В работе";
        }

        public void SetCompleted()
        {
            _task.Status = "Выполнена";
        }
    }
}

