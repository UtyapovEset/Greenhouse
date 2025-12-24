using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Greenhose
{
    internal interface ITaskStatusAdapter
    {
        void SetPlanned();
        void SetInProgress();
        void SetCompleted();
    }
}
