using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCICommon
{
    [Serializable]
    public class HardwareConfiguration
    {
        private bool[] expander_0_configuration = new bool[16];
        private bool[] expander_1_configuration = new bool[16];

        public HardwareConfiguration()
        {

        }
        
        public HardwareConfiguration(bool[] expander_0_configuration, bool[] expander_1_configuration)
        {
            this.expander_0_configuration = expander_0_configuration;
            this.expander_1_configuration = expander_1_configuration;
        }

        public bool[] Expander0Configuration
        {
            get
            {
                return expander_0_configuration;
            }
            set
            {
                expander_0_configuration = value;
            }
        }

        public bool[] Expander1Configuration
        {
            get
            {
                return expander_1_configuration;
            }
            set
            {
                expander_1_configuration = value;
            }
        }


    }
}
