using System;
using QBFC16Lib;

namespace oneXerpQB
{
    public class Vendor
    {
        private string _name;
        private string _companyName;
        private Address _vendorAddress;
        private string _phone;

        public Vendor(string name, string companyName, Address vendorAddress, string phone)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Name cannot be null or whitespace.", nameof(name));
            }

            _name = name;
            _companyName = companyName;
            _vendorAddress = vendorAddress;
            _phone = phone;
        }
    }
}
