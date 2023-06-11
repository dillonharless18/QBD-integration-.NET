using System;

namespace oneXerpQB
{
    public class Address
    {
        private string _addr1;
        private string _addr2;
        private string _addr3;
        private string _addr4;
        private string _addr5;
        private string _city;
        private string _state;
        private string _postalCode;
        private string _country;
        private string _note;

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
