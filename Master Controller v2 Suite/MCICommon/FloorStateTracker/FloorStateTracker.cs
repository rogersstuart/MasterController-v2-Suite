using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace MCICommon
{
    public class FloorStateTracker
    {
        public event FreshStateAvailable freshstateevent;
        public delegate void FreshStateAvailable(BuildingFloor f);

        public Object event_lock = new Object();
        public Object client_lock = new Object();

        private MqttClient client = null;

        private Object floor_state_access_lock = new Object();

        private BuildingFloor[] floors = new BuildingFloor[14];

        private int car_num;

        private Task tracker_task;
        private Task upkeep_task;

        private bool isActive = false;

        private DateTime last_refresh = DateTime.MinValue;
        private TimeSpan refresh_interval = TimeSpan.FromMilliseconds(100);

        public FloorStateTracker(int car_num)
        {
            this.car_num = car_num;
            Start();
        }

        public void Start()
        {
            isActive = true;
            
            //init floors
            for (int i = 1; i < 15; i++)
                floors[i - 1] = new BuildingFloor(i);

            upkeep_task = GenerateUpkeepTask();
            upkeep_task.Start();
        }

        public void Stop()
        {
            isActive = false;

            while (!upkeep_task.IsCompleted) ;
            while (!tracker_task.IsCompleted) ;
        }

        //maintain connection
        public Task GenerateUpkeepTask()
        {
            return new Task(async () =>
            {
                while(isActive)
                {
                    try
                    {
                        if (client == null || client.IsConnected == false)
                            InitClient();
                    }
                    catch (Exception ex) { }

                    
                    await Task.Delay(1000);
                }
            });
        }

        private void InitClient()
        {
            client = new MqttClient(MCv2Persistance.Instance.Config.MQTTBroker.AddressString);

            client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
            var guid = Guid.NewGuid();
            string clientId = guid.ToString();

            for (int i = 1; i < 15; i++)
                client.Subscribe(new string[] { "access_control/elevator/car_" + car_num + "/floor_" + i + "/get" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });

            client.Connect(clientId);
        }

        public Task GenerateTrackerTask()
        {
            return new Task(async () =>
            {
                while(isActive)
                {
                    if (DateTime.Now - last_refresh >= refresh_interval)
                        RefreshState();
                    else
                        await Task.Delay(1);
                }
            });
        }

        private void RefreshState()
        {
            DateTime evaluation_time = DateTime.Now;

            Parallel.ForEach(floors, f => 
            {
                //evaluate all modifiers for expiration and removal
                f.modifiers.RemoveAll(x => x.modifier_expiration_condition(evaluation_time));

                bool result = false;

                //evaluate system level modifier
                if(f.system_modifier_activation_condition(evaluation_time))
                {
                    //use system modifier

                    result = f.system_modifier_result(evaluation_time);
                }
                else
                {
                    //use alternative modifiers

                    //evaluate 0-max
                    var precidence_levels = f.modifiers.Select(x => x.precidence).Distinct().OrderBy(y => y);

                    //evaluate each level
                    foreach(var level in precidence_levels)
                    {
                        //observe transparencies
                        var current_round = f.modifiers.Where(x => x.precidence == level).Where(y => y.modifier_activation_condition(evaluation_time));

                        //evaluate opaque modifiers
                        if (current_round.Count() > 0)
                        {
                            var results = current_round.Select(x => x.modifier_result(evaluation_time));
                            var true_votes = results.Where(x => x == true).Count();
                            var false_votes = results.Where(x => x == false).Count();

                            if (true_votes == 0 && false_votes == 0)
                                continue;
                            else
                                if (true_votes == false_votes)
                                    result = f.arbitration_function(evaluation_time);
                                else
                                    if (true_votes > false_votes)
                                        result = true;
                                    else
                                        if (false_votes > true_votes)
                                            result = false;

                            break;
                        }
                        else
                            continue;
                    }    
                }

                if (result != f.last_reported_state)
                    SetFloorState(f.floor_number, result);

            });
        }

        private void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            Task.Run(() =>
            {
                Parallel.ForEach(floors, f =>
                {
                    if (e.Topic == ("access_control/elevator/car_" + car_num + "/floor_" + f.floor_number + "/get"))
                    {
                        lock (f.access_lock)
                        {
                            f.last_reported_state = e.Message[0] == '0' ? false : true;

                            lock(event_lock)
                                if(freshstateevent != null)
                                    freshstateevent(f);
                        }
                    }
                });
            });
        }

        public void LockFloor(int number)
        {
            lock(client_lock)
                client.Publish("access_control/elevator/car_" + car_num + "/floor_" + number + "/set", new byte[] { (byte)'0' });
        }

        public void UnlockFloor(int number)
        {
            lock (client_lock)
                client.Publish("access_control/elevator/car_" + car_num + "/floor_" + number + "/set", new byte[] { (byte)'1' });
        }

        public void SetFloorState(int number, bool state)
        {
            if (state)
                UnlockFloor(number);
            else
                LockFloor(number);
        }

        public void EnqueueModifier()
        {

        }

        public bool GetFloorState(int number)
        {
            lock(floors[number - 1].access_lock)
                return floors[number - 1].last_reported_state;
        }

        public void ToggleFloor(int floor)
        {

        }
    }  
}
