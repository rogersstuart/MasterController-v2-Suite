using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using MCICommon;

namespace MasterControllerDotNet_Server
{
    [Serializable]
    public class PanelMonitor
    {
        public event NewStateAvailable newstate;
        public delegate void NewStateAvailable(PanelMonitor pnlmon, PanelEventArgs pnlevargs);

        public event CardPresented presented;
        public delegate void CardPresented(PanelMonitor pnlmon, PanelEventArgs pnlevargs);

        ConnectionProperties connprop;
        
        [NonSerialized] PanelConnection connection;

        Object state_lock = new Object();
        PanelState state = null;

        [NonSerialized] Thread monitor_thread;
        volatile bool running;

        int associated_car = 0;

        public PanelMonitor(string ip, int port) : this(new ConnectionProperties(ip, port)) { }

        public PanelMonitor(ConnectionProperties connprop)
        {
            this.connprop = connprop;
        }

        private Thread GenerateMonitor()
        {
            return new Thread(async delegate()
            {
                running = true;
                while (running)
                    await Refresh();
            });
        }

        private Task Refresh()
        {
            return Task.Run(delegate()
            {
                UInt64 uid, uptime;
                DateTime uid_read_timestamp;

                PanelCommandHandle pch = connection.EnqueueCommand(new PanelCommand(PanelCommand.POLL_CARD_PRESENCE));
                pch.Handle.WaitOne(); //uid will be waiting
                uid_read_timestamp = DateTime.Now;

                byte[] conversion_buffer = new byte[8];
                Array.Copy(pch.Command.RxPacket, 1, conversion_buffer, 1, 7);
                Array.Reverse(conversion_buffer);

                uid =  BitConverter.ToUInt64(conversion_buffer, 0);

                if (uid != 0)
                    connection.EnqueueCommand(new PanelCommand(PanelCommand.CLEAR_CARD)).Handle.WaitOne();

                pch = connection.EnqueueCommand(new PanelCommand(PanelCommand.GET_UPTIME));
                pch.Handle.WaitOne(); //uid will be waiting
                uptime = BitConverter.ToUInt64(pch.Command.RxPacket, 1);

                PanelState stcpy = null;
                lock (state_lock)
                    if(state != null)
                        stcpy = new PanelState(state);
                    else
                        state = new PanelState(new AccessControlCard(uid, AccessControlCard.MIFARE_CLASSIC, uid_read_timestamp), uptime, uid_read_timestamp);

                if(stcpy != null)
                {
                    AccessControlCard accessctlcd = stcpy.Card;

                    //if this is a different card then continue
                    if (accessctlcd.UID != uid)
                    {
                        accessctlcd = new AccessControlCard(uid, AccessControlCard.MIFARE_CLASSIC, uid_read_timestamp);

                        if(uid != 0)
                            if(presented != null)
                                presented(this, new PanelEventArgs(new PanelState(accessctlcd, uptime, uid_read_timestamp)));
                    }

                    lock (state_lock)
                        state = new PanelState(accessctlcd, uptime, uid_read_timestamp);

                    if (newstate != null)
                        newstate(this, new PanelEventArgs(new PanelState(accessctlcd, uptime, uid_read_timestamp)));
                }

            });
        }

        public PanelState PanelState
        {
            get
            {
                lock (state_lock)
                    return state == null ? null : new PanelState(state);
            }
        }

        public void ResetPanel()
        {

        }

        public Task AnimateLED(byte animation_id)
        {
            return Task.Run(() =>
            { 
                PanelCommand pnlcmd = new PanelCommand(PanelCommand.DISPLAY_ANIMATION, animation_id);

                connection.EnqueueCommand(pnlcmd).Handle.WaitOne();
            });
        }

        public void Stop() { Stop(false); }

        public void Stop(bool ignoreconn)
        {
            if (monitor_thread != null)
            {
                running = false;
                while (monitor_thread.IsAlive) ;
            }

            if (connection != null && !ignoreconn)
            {
                while (connection.Busy) ;
                connection.Stop();
                connection = null;
            }
        }

        public int AssociatedCar
        {
            get
            {
                return associated_car;
            }

            set
            {
                associated_car = value;
            }
        }

        public void Start() { Start(false); }

        //the ignore connection flag with prevent reinitilization of the connection if the connection is not null
        public void Start(bool ignoreconn)
        {
            if (connection != null && !ignoreconn)
                Stop(false);

            if (connection == null)
                connection = new PanelConnection(connprop);

            monitor_thread = GenerateMonitor();
            monitor_thread.Start();
        }

        public override string ToString()
        {
            if (connection != null)
                return "Panel (Active:" + associated_car + ") " + connprop.IPAddress + ":" + connprop.TCPPort;
            else
                return "Panel (Inactive:" + associated_car + ") " + connprop.IPAddress + ":" + connprop.TCPPort;
        }
    }
}
