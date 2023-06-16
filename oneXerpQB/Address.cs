using System;

namespace oneXerpQB
{
    public class Address
    {
        internal string _addr1 { get; set; }
        internal string _addr2 { get; set; }
        internal string _addr3 { get; set; }
        internal string _addr4 { get; set; }
        internal string _addr5 { get; set; }
        internal string _city { get; set; }
        internal string _state { get; set; }
        internal string _postalCode { get; set; }
        internal string _country { get; set; }
        internal string _note { get; set; }

        public Address(string addr1, string addr2, string addr3, string addr4, string addr5,
            string city, string state, string postalCode, string country, string note)
        {
            _addr1 = addr1;
            _addr2 = addr2;
            _addr3 = addr3;
            _addr4 = addr4;
            _addr5 = addr5;
            _city = city;
            _state = state;
            _postalCode = postalCode;
            _country = country;
            _note = note;
        }
    }
}
