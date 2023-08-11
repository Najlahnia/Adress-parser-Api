using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AddressParserLib.Models;

namespace AddressParserLib
{
    public abstract class AdressParserAbstract
    {
        public abstract AddressParseResult ParseAddress(string input, Regex pattern, bool normalize);

        public abstract AddressParseResult ParseAddress(string input);

        public abstract AddressParseResult ParseAddress(string input, bool normalize);
    }
}
