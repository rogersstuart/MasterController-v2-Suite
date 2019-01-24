using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterControllerInterface
{
    public class MCIv2User
    {
        private ulong user_id = 0;
        private string name = "";
        private string description = "";

        public MCIv2User(MCIv2User user) : this(user.UserID, user.Name, user.Description) { }

        public MCIv2User(ulong user_id, string name, string description)
        {
            if (user_id == 0 || (name == null && description == null) || (name == "" && description == ""))
                throw new Exception("One or more constructor arguments are invalid.");

            this.user_id = user_id;
            this.name = name == null ? "" : name;
            this.description = description == null ? "" : description;
        }

        public string Name{ get { return name; } set { name = value; } }

        public string Description { get { return description; } set { description = value; } }

        public ulong UserID { get { return user_id; }
            set
            {
                if (value == 0)
                    throw new Exception("The User ID Supplied Is Invalid");
                user_id = value;
            }
        }
    }
}
