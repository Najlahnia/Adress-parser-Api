using System;
using System.Text.RegularExpressions;

namespace AddressEstimation
{
    class Program
    {
        static void Main(string[] args)
        {
            string addressInput = GetUserInput("Enter an address: ");
            Address parsedAddress = ParseAddress(addressInput);

            if (parsedAddress != null)
            {
                Console.WriteLine("Parsed Address:");
                Console.WriteLine($"Street: {parsedAddress.Street}");
                Console.WriteLine($"City: {parsedAddress.City}");
                Console.WriteLine($"Province: {parsedAddress.Province}");
                Console.WriteLine($"Postal Code: {parsedAddress.PostalCode}");
            }
            else
            {
                Console.WriteLine("Invalid address format.");
            }
        }

        static Address ParseAddress(string address)
        {
            string pattern = @"^(?<Street>.*),\s*(?<City>.*),\s*(?<Province>.*),\s*(?<PostalCode>.*)$";

            Match match = Regex.Match(address, pattern);

            if (match.Success)
            {
                Address parsedAddress = new Address
                {
                    Street = match.Groups["Street"].Value.Trim(),
                    City = match.Groups["City"].Value.Trim(),
                    Province = match.Groups["Province"].Value.Trim(),
                    PostalCode = match.Groups["PostalCode"].Value.Trim()
                };

                return parsedAddress;
            }
            else
            {
                return null;
            }
        }

        static string GetUserInput(string prompt)
        {
            Console.Write(prompt);
            return Console.ReadLine();
        }
    }

    class Address
    {
        public string Street { get; set; }
        public string City { get; set; }
        public string Province { get; set; }
        public string PostalCode { get; set; }
    }
}
