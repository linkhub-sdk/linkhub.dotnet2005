using System;
using System.Collections.Generic;
using System.Text;
using System.Json;

namespace Linkhub
{
    public class Token
    {
        public String session_token;
        public String serviceID;
        public String linkID;
        public String usercode;
        public String ipaddress;
        public String expiration;
        public List<String> scope;
    }
}
