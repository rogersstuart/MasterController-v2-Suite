using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterControllerInterface
{
    public class MCIv2UserCollection : List<MCIv2User>
    {
        private DateTime creation_timestamp;

        public MCIv2UserCollection(List<MCIv2User> users) : base(users)
        {
            creation_timestamp = DateTime.Now;
        }

        public DateTime CreationTimestamp
        {
            get
            {
                return creation_timestamp;
            }
        }
    }
}
