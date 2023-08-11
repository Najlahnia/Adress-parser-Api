using System.Resources;
using System.Runtime.ConstrainedExecution;
using AddressParserLib;

var parserUsa = AddressParser.Default;
bool loop;

do
{
    Console.WriteLine("Please enter your adress");
    var input = Console.ReadLine();

    var result = parserUsa.ParseAddress(input);
    if (result == null)
    {  
        Console.WriteLine("Resources.ErrorNotParsable");
    }
    
    else
    {
        var properties = result
            .GetType()
            .GetProperties()
            .OrderBy(x => x.Name);
        foreach (var property in properties)
        {
            Console.WriteLine(
                "{0,30} : {1}",
                property.Name,
                property.GetValue(result, null));
        }
    }

    var readLine = Console.ReadLine() ?? string.Empty;
    loop = readLine.ToUpperInvariant() == "Y";
}
while (loop);
