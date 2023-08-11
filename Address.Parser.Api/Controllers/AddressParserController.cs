using Microsoft.AspNetCore.Mvc;
using AddressParserLib;
using System.Linq;
using Address.Parser.Api.Models;
using Address.Parser.Api.Helpers;

namespace AddressParserAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AddressController : ControllerBase
    {
        [HttpPost("_parse")]
        public IActionResult ParseAddress([FromBody] AddressToParseDto addressToParseDto)
        {
            var parserUsa = AddressParser.Default;
            var result = parserUsa.ParseAddress(addressToParseDto.Address, true);

            if (result == null)
            {
                return BadRequest("Error: Address could not be parsed.");
            }
            var addressResult = result.Map();
 

            return Ok(addressResult);
        }


        // Nouvel endpoint pour les adresses canadiennes
        [HttpPost("_parseCanada")] 
        public IActionResult ParseCanadaAddress([FromBody] AddressToParseDto addressToParseDto)
        {
            var parserCanada = AddressParserCanada.Default; // Assumer que vous avez un AddressParserCanada défini
            var result = parserCanada.ParseAddress(addressToParseDto.Address, true);

            if (result == null)
            {
                return BadRequest("Error: Canadian address could not be parsed.");
            }
            var addressResult = result.Map();

            return Ok(addressResult);
        }
    }
}
