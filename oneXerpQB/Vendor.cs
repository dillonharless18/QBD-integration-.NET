﻿using System;
using QBFC16Lib;

namespace erpQB
{
    public class Vendor : ErpObject
    {
        public string Name { get; set; }
        public string CompanyName { get; set; }
        public Address VendorAddress { get; set; }
        public string Phone { get; set; } 

        public Vendor(string name, string companyName, Address vendorAddress, string phone)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Name cannot be null or whitespace.", nameof(name));
            }

            Name = name;
            CompanyName = companyName;
            VendorAddress = vendorAddress;
            Phone = phone;
        }
    }
}
