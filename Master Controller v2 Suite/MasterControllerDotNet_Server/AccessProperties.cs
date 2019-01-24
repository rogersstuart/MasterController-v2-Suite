using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MCICommon;

namespace MasterControllerDotNet_Server
{
    [Serializable]
    public class AccessProperties
    {
        private bool force_enable, force_disable;
        private DateTime enabled_from, enabled_to;
        
        private List<ActivationProperties> actiprops;

        public AccessProperties(DateTime enabled_from, DateTime enabled_to, List<ActivationProperties> actiprops, bool force_enable, bool force_disable)
        {
            this.enabled_from = enabled_from;
            this.enabled_to = enabled_to;
            this.actiprops = actiprops;
            this.force_enable = force_enable;
            this.force_disable = force_disable;
        }

        public List<ExpanderModificationProperties> ProcessActivations()
        {
            List<ExpanderModificationProperties> exprevprop = new List<ExpanderModificationProperties>();

            foreach (ActivationProperties actiprop in actiprops)
            {
                if (actiprop.Evaluate())
                {
                    exprevprop.Add(new ExpanderModificationProperties(actiprop.Exp0Mask, actiprop.Exp1Mask, actiprop.Exp0Values, actiprop.Exp1Values,
                                    actiprop.RevertAfter > 0 ? DateTime.Now.AddSeconds(actiprop.RevertAfter) : actiprop.RangeEnd, 0));
                }
            }

            return exprevprop;
        }

        public bool ForceEnable
        {
            get { return force_enable; }

            set { force_enable = value; }
        }

        public bool ForceDisable
        {
            get { return force_disable; }

            set { force_disable = value; }
        }

        public DateTime EnabledFrom
        {
            get { return enabled_from; }

            set { enabled_from = value; }
        }

        public DateTime EnabledTo
        {
            get { return enabled_to; }

            set { enabled_to = value; }
        }

        public List<ActivationProperties> ActivationProperties
        {
            get { return actiprops; }
        }
    }
}
