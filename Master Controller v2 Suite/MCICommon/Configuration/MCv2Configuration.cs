using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;

namespace MCICommon
{
    //[DataContract]
    public class BackupConfiguration
    {
        private DateTime last_nag_backup_occured = DateTime.MinValue;
        private DateTime last_auto_backup_occured = DateTime.MinValue;
        private TimeSpan backup_nag_interval = TimeSpan.FromDays(7);
        private bool enable_backup_nag = false;
        private bool enable_auto_backup = true;

        public BackupConfiguration() { }

        //[DataMember]
        public bool EnableAutoBackup
        {
            get { return enable_auto_backup; }
            set { enable_auto_backup = value; }
        }

        //[DataMember]
        public bool EnableBackupNag
        {
            get { return enable_backup_nag; }
            set { enable_backup_nag = value; }
        }

        //[DataMember]
        public DateTime LastNagBackupTimestamp
        {
            get { return last_nag_backup_occured; }
            set { last_nag_backup_occured = value; }
        }

        //[DataMember]
        public DateTime LastAutoBackupTimestamp
        {
            get { return last_auto_backup_occured; }
            set { last_auto_backup_occured = value; }
        }

        //[DataMember]
        public TimeSpan BackupNagInterval
        {
            get { return backup_nag_interval; }
            set { backup_nag_interval = value; }
        }
    }

    //[DataContract]
    public class DatabaseConfiguration
    {
        private DatabaseConnectionProperties dbconnprop = new DatabaseConnectionProperties("", "", "", "");
        private int num_cached_db_connections = 8;
        private TimeSpan db_cache_refresh_interval = TimeSpan.FromMinutes(10); //minutes

        private bool use_interleaving = true;
        private TimeSpan interleaving_dynamic_range = TimeSpan.FromMinutes(10);

        public DatabaseConfiguration()
        {

        }

        //[DataMember]
        public DatabaseConnectionProperties DatabaseConnectionProperties
        {
            get { return dbconnprop; }
            set { dbconnprop = value; }
        }

        //[DataMember]
        public int NumCachedDBConnections
        {
            get { return num_cached_db_connections; }
            set { num_cached_db_connections = value; }
        }

        //[DataMember]
        public TimeSpan DBCacheRefreshInterval
        {
            get { return db_cache_refresh_interval; }
            set { db_cache_refresh_interval = value; }
        }

        //[DataMember]
        public bool UseInterleaving
        {
            get { return use_interleaving; }
            set { use_interleaving = value; }
        }

        //[DataMember]
        public TimeSpan InterleavingDynamicRange
        {
            get { return interleaving_dynamic_range; }
            set { interleaving_dynamic_range = value; }
        }

        //[DataContract]
        public class UIConfiguration
        {
            private int user_lookup_holdoff_time = 1000; //milliseconds
            private bool show_dialog_on_mcv2_offline_controller_interaction_success = false;
            private bool show_dialog_on_mcv2_offline_controller_interaction_failure = true;
            private bool encode_displayed_uids_flag = true;
            private bool warn_if_not_administrator = false;
            public ulong SelectedGroup { get; set; } = 0;

            public UIConfiguration()
            {

            }

            //[DataMember]
            public bool WarnIfNotAdministrator
            {
                get { return warn_if_not_administrator; }
                set { warn_if_not_administrator = value; }
            }


            //[DataMember]
            public int UserLookupHoldoff
            {
                get { return user_lookup_holdoff_time; }
                set { user_lookup_holdoff_time = value; }
            }

            //[DataMember]
            public bool EncodeDisplayedUIDsFlag
            {
                get { return encode_displayed_uids_flag; }
                set { encode_displayed_uids_flag = value; }
            }

            //[DataMember]
            public bool ShowDialogOnMCV2OfflineControllerInteractionSuccess
            {
                get { return show_dialog_on_mcv2_offline_controller_interaction_success; }
                set { show_dialog_on_mcv2_offline_controller_interaction_success = value; }
            }

            //[DataMember]
            public bool ShowDialogOnMCV2OfflineControllerInteractionFailure
            {
                get { return show_dialog_on_mcv2_offline_controller_interaction_failure; }
                set { show_dialog_on_mcv2_offline_controller_interaction_failure = value; }
            }
        }

        //[DataContract]
        public class MCv2Configuration
        {
            private string guid = "";

            private BackupConfiguration back_cfg = new BackupConfiguration();
            private DatabaseConfiguration db_config = new DatabaseConfiguration();
            private UIConfiguration ui_config = new UIConfiguration();

            private bool sync_time_after_uploads_flag = false;
            private int device_server_port = 10249;
            private TCPConnectionProperties mqttconnprop = new TCPConnectionProperties("MQTT Broker", "mccsrv1.mct", 1833);
            private ulong online_controller_list = 15125086423725366287;
            private bool auto_login = true;

            public MCv2Configuration() { }

            //[DataMember]
            public string GUID
            {
                get
                {
                    if (guid == null || guid.Trim() == "")
                        guid = Guid.NewGuid().ToString();

                    return guid;
                }
                set { guid = value; }
            }

            //[DataMember]
            public BackupConfiguration BackupConfiguration
            {
                get { return back_cfg; }
                set { back_cfg = value; }
            }

            //[DataMember]
            public DatabaseConfiguration DatabaseConfiguration
            {
                get { return db_config; }
                set { db_config = value; }
            }

            //[DataMember]
            public UIConfiguration UIConfiguration
            {
                get { return ui_config; }
                set { ui_config = value; }
            }

            //[DataMember]
            public int DeviceServerPort
            {
                get { return device_server_port; }
                set { device_server_port = value; }
            }




            //[DataMember]
            public bool SyncTimeAfterUploadsFlag
            {
                get { return sync_time_after_uploads_flag; }
                set { sync_time_after_uploads_flag = value; }
            }



            //[DataMember]
            public TCPConnectionProperties MQTTBroker
            {
                get { return mqttconnprop; }
                set { mqttconnprop = value; }
            }

            //[DataMember]
            public ulong OnlineControllerList
            {
                get { return online_controller_list; }
                set { online_controller_list = value; }
            }


            public bool AutoLogin
            {
                get { return auto_login; }
                set { auto_login = value; }
            }

        }
    }
}
