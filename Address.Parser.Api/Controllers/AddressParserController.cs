using Microsoft.AspNetCore.Mvc;
using AddressParserLib;
using Address.Parser.Api.Models;
using Address.Parser.Api.Helpers;

namespace AddressParserAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AddressApiController : ControllerBase
    {
        private readonly AddressParser _parserUsa = AddressParser.Default;
        private readonly AddressParserCanada _parserCanada = AddressParserCanada.Default;

        [HttpPost("parse")]
        public IActionResult ParseAddress([FromBody] AddressToParseDto addressToParseDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid input data.");
            }

            var result = _parserUsa.ParseAddress(addressToParseDto.Address, true);

            if (result == null)
            {
                return BadRequest("Error: USA Address could not be parsed.");
            }

            var addressResult = result.Map();
            return Ok(addressResult);
        }

        [HttpPost("parseCanada")]
        public IActionResult ParseCanadaAddress([FromBody] AddressToParseDto addressToParseDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid input data.");
            }

            var result = _parserCanada.ParseAddress(addressToParseDto.Address, true);

            if (result == null)
            {
                return BadRequest("Error: Canadian address could not be parsed.");
            }

            var addressResult = result.Map();
            return Ok(addressResult);
        }
    }
}
