using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCICommon
{
    public class SystemScheduler
    {
        List<ScheduleEvent> schedule_events = new List<ScheduleEvent>();
        string access_string;
        Task schedule_watcher;

        public SystemScheduler()
        {

        }
    }

    public class ExpanderScheduler : SystemScheduler
    {
        public ExpanderScheduler()
        {

        }
    }
}
