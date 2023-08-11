using Address.Parser.Api.Models;
using AddressParserLib.Models;

namespace Address.Parser.Api.Helpers
{
    public static class AddressHelper
    {//methode d'extention
        public static AddressDto Map(this AddressParseResult address)
        {
            return new AddressDto()
            {
                City = address.City,
                Number = address.Number,
                Postdirectional = address.Postdirectional,
                Predirectional = address.Predirectional,
                SecondaryNumber = address.SecondaryNumber,
                SecondaryUnit = address.SecondaryUnit,
                State = address.State,
                Street = address.Street,
                StreetLine = address.StreetLine,
                ZipCode =  address.Zip,
            };
        }
    }
}
