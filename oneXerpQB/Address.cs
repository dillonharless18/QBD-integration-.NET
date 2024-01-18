using System;

namespace erpQB
{
    public class Address
    {
        public string _addr1 { get; set; }
        public string _addr2 { get; set; }
        public string _addr3 { get; set; }
        public string _addr4 { get; set; }
        public string _addr5 { get; set; }
        public string _city { get; set; }
        public string _state { get; set; }
        public string _postalCode { get; set; }
        public string _country { get; set; }
        public string _note { get; set; }

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
