using System;
using System.Collections.Generic;
using System.Text;

namespace System.Json
{
    public class JsonMapFieldName : System.Attribute
    {
        private string _name;

        public string name
        {
            get { return _name; }
        }

        public JsonMapFieldName(string name)
        {
            this._name = name;
        }
    }
}
