using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using MySql.Data.MySqlClient;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Linq;
using System.Collections.Concurrent;
using MCICommon;

namespace MasterControllerDotNet_Server
{
    public class AltMonitor
    {
        DatabaseConnectionProperties dbconnprop;
        string table_name;
        
        ExpanderMonitor expmon;
        List<PanelMonitor> pnlmons;

        List<AccessControlProcessingCollection> processing_queue = new List<AccessControlProcessingCollection>();

        BlockingCollection<ExpanderModificationProperties> expmodprops = new BlockingCollection<ExpanderModificationProperties>();

        Thread monitor_thread;
        volatile bool monitor_active;

        BinaryFormatter bf = new BinaryFormatter();

        public AltMonitor(ExpanderMonitor expmon, List<PanelMonitor> pnlmons, DatabaseConnectionProperties dbconnprop, string table_name)
        {
            this.expmon = expmon;
            this.pnlmons = pnlmons;
            this.dbconnprop = dbconnprop;
            this.table_name = table_name;
        }

        public void Start()
        {
            foreach(PanelMonitor pnlmon in pnlmons)
                pnlmon.presented += NewCardPresented;

            monitor_active = true;
            monitor_thread = GenerateMonitorThread();
            monitor_thread.Start();
        }

        public void Stop()
        {
            foreach (PanelMonitor pnlmon in pnlmons)
                pnlmon.presented -= NewCardPresented;

            if (monitor_thread != null)
            {
                monitor_active = false;
                while (monitor_thread.IsAlive) ;
                monitor_thread = null;
            }
        }

        private void NewCardPresented(PanelMonitor pnlmon, PanelEventArgs pnlevargs)
        {   
            try
            {
            using (MySqlConnection sqlconn = new MySqlConnection(dbconnprop.ConnectionString))
            {
                sqlconn.Open();

                //lookup
                using (MySqlCommand cmdName = new MySqlCommand("select data from `" + table_name + "` where uid=" + pnlevargs.PanelState.Card.UID, sqlconn))
                using (MySqlDataReader reader = cmdName.ExecuteReader())
                {

                    if (reader.Read())
                    {
                        if (reader[0] != DBNull.Value)
                        {
                            byte[] bytes = (byte[])reader["data"];

                            AccessProperties accessprop;
                            using (MemoryStream ms = new MemoryStream())
                            {
                                ms.Write(bytes, 0, bytes.Length);
                                ms.Position = 0;
                                accessprop = (AccessProperties)bf.Deserialize(ms);
                            }

                            //add to processing queue
                            if (!accessprop.ForceDisable)
                            {
                                if (accessprop.EnabledFrom <= DateTime.Now || accessprop.ForceEnable)
                                {
                                    if (accessprop.EnabledTo >= DateTime.Now || accessprop.ForceEnable)
                                    {
                                        //card is active; continue verifying

                                        List<ExpanderModificationProperties> temp = accessprop.ProcessActivations();
                                        int associated_car = pnlmon.AssociatedCar;

                                        if (temp.Count > 0)
                                        {
                                            pnlmon.AnimateLED(PanelCommand.ANIMATION_APPROVAL);

                                            PanelMonitor monitor = pnlmon;
                                            AccessControlCard card = pnlevargs.PanelState.Card;

                                            Task.Run(delegate()
                                            {
                                                AccessControlLogManager.AddAccessControlLogEntry(card.UID, new AccessControlLogEntry(DateTime.Now, "User Approved Entry", card,
                                                    expmon.ToString(), monitor.ToString()));
                                            });

                                        }
                                        else
                                        {
                                            pnlmon.AnimateLED(PanelCommand.ANIMATION_DECLINE);

                                            PanelMonitor monitor = pnlmon;
                                            AccessControlCard card = pnlevargs.PanelState.Card;

                                            Task.Run(delegate()
                                            {
                                                AccessControlLogManager.AddAccessControlLogEntry(card.UID, new AccessControlLogEntry(DateTime.Now, "User Declined Entry", card,
                                                    expmon.ToString(), monitor.ToString()));
                                            });

                                        }

                                        foreach (ExpanderModificationProperties expmodprop in temp)
                                        {
                                            expmodprop.AssociatedCar = associated_car;
                                            expmodprops.Add(expmodprop);
                                        }
                                    }
                                    else
                                        pnlmon.AnimateLED(PanelCommand.ANIMATION_DECLINE);
                                }
                                else
                                    pnlmon.AnimateLED(PanelCommand.ANIMATION_DECLINE);
                            }
                            else
                                pnlmon.AnimateLED(PanelCommand.ANIMATION_DECLINE);
                        }
                    }
                    else
                        pnlmon.AnimateLED(PanelCommand.ANIMATION_DECLINE);

                }
            }
            }
                catch (MySqlException mysqlex)
                {
                    ErrorLogManager.AppendLog("AltMonitor - MySqlException Occured During Card Lookup", true);
                }
                catch (Exception ex)
                {
                    ErrorLogManager.AppendLog("AltMonitor - General Error Occured During Card Lookup", true);
                }
        }

        private Thread GenerateMonitorThread()
        {
            return new Thread(async delegate()
            {
                while (monitor_active)
                {
                    //iterate through the expandermodificationproperties
                    //sort by priority
                    //compile into value to write
                    //write value to the expanders
                    if (expmodprops.Count > 0)
                    {
                        //expmodprops = expmodprops.OrderByDescending(o => o.StackPriority).ToList(); //probably not necessary, priorty is useless right now (and always?)

                        bool[] compiled_exp0mask = { true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true };
                        bool[] compiled_exp1mask = { true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true };
                        bool[] compiled_exp0vals = expmon.DefaultState.Expander0State;
                        bool[] compiled_exp1vals = expmon.DefaultState.Expander1State;

                        for (int index_counter = expmodprops.Count-1; index_counter >= 0; index_counter--)
                        {
                            ExpanderModificationProperties expmodprop = expmodprops.ElementAt(index_counter);

                            if (expmodprop.Evaluate()) //true means its still active
                            {
                                for (int bool_counter = 0; bool_counter < 16; bool_counter++)
                                {
                                    if (expmodprop.AssociatedCar == 0)
                                        compiled_exp0mask[bool_counter] = index_counter == expmodprops.Count - 1 ? compiled_exp0mask[bool_counter] & expmodprop.Exp0Mask[bool_counter] : compiled_exp0mask[bool_counter] |= expmodprop.Exp0Mask[bool_counter];
                                    if (expmodprop.AssociatedCar == 1)
                                        compiled_exp1mask[bool_counter] = index_counter == expmodprops.Count - 1 ? compiled_exp1mask[bool_counter] & expmodprop.Exp1Mask[bool_counter] : compiled_exp1mask[bool_counter] |= expmodprop.Exp1Mask[bool_counter];
                                    
                                    if (expmodprop.AssociatedCar == 0)
                                        compiled_exp0vals[bool_counter] |= expmodprop.Exp0Values[bool_counter];
                                    if (expmodprop.AssociatedCar == 1)
                                        compiled_exp1vals[bool_counter] |= expmodprop.Exp1Values[bool_counter];
                                }
                            }
                            else
                            {
                                //if there are remaing items include the mask from the removed item so its values will be restored
                                if (expmodprops.Count > 1)
                                    for (int bool_counter = 0; bool_counter < 16; bool_counter++)
                                    {
                                        if (expmodprop.AssociatedCar == 0)
                                            compiled_exp0mask[bool_counter] = index_counter == expmodprops.Count - 1 ? compiled_exp0mask[bool_counter] & expmodprop.Exp0Mask[bool_counter] : compiled_exp0mask[bool_counter] |= expmodprop.Exp0Mask[bool_counter];

                                        if (expmodprop.AssociatedCar == 1)
                                            compiled_exp1mask[bool_counter] = index_counter == expmodprops.Count - 1 ? compiled_exp1mask[bool_counter] & expmodprop.Exp1Mask[bool_counter] : compiled_exp1mask[bool_counter] |= expmodprop.Exp1Mask[bool_counter];
                                    }

                                //set the value to null for removal later
                                expmodprops.TryTake(out expmodprop);
                            }
                        }

                        await expmon.WriteExpanders(compiled_exp0mask, compiled_exp1mask, compiled_exp0vals, compiled_exp1vals);
                        Console.WriteLine("wrote expanders from monitor");
                    }

                    Thread.Sleep(100);
                }
            });
        }
    }
}
