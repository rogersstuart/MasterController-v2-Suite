using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimulationCore.DoorControl
{
    internal static class DCSim
    {
        private static Task dc_sim_task;
        private static CancellationTokenSource dc_sim_task_cancellation_source;

        internal static void Start()
        {
            if (dc_sim_task == null || dc_sim_task.Status != TaskStatus.Running)
            {
                dc_sim_task_cancellation_source = new CancellationTokenSource();
                dc_sim_task = GenerateDCSimTask();
                dc_sim_task.Start();
            }
            else
                throw new Exception("The task is already running.");
        }
        internal static void Stop()
        {
            dc_sim_task_cancellation_source.Cancel();
        }

        private static Task GenerateDCSimTask()
        {
            return new Task(() =>
            {
                HttpListener listner = new HttpListener();

                
                while(!dc_sim_task_cancellation_source.Token.IsCancellationRequested)
                {
                    //
                }


            }, dc_sim_task_cancellation_source.Token);
        }
    }
}
