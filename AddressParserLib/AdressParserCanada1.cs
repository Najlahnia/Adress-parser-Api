// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AddressParser.cs" company="Jim Counts">
//     Copyright (c) Jim Counts 2013.
// </copyright>
// <summary>
//   Defines the AddressParser type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;

namespace canadaAddress
{
    
        /// <summary>
        /// Gets the zip pattern.
        /// </summary>
        /// <value>
        /// The zip pattern.
        /// </value>
        /// l'expression régulière @"\d{5}(?:-?\d{4})?" valide les codes postaux américains qui ont soit cinq chiffres consécutifs, 
        /// soit cinq chiffres suivis d'un tiret et de quatre chiffres supplémentaires.
        /// 

        //Zip sous forme canadieene  "ANA NAN".
        //public static string ZipPattern => @"[A-CEGHJ-NPR-TVXYa-ceghj-npr-tvxy]\d[A-CEGHJ-NPR-TVXYa-ceghj-npr-tvxy] \d[A-CEGHJ-NPR-TVXYa-ceghj-npr-tvxy]\d";
    }
//<summary>
//var parserUsa = AddressParser.Default;

//bool loop;

//do
//{
  //  Console.WriteLine("Please enter your adress");
    //var input = Console.ReadLine();

    //var result = parserUsa.ParseAddressCanada(input);
    //if (result == null)
   // {
        // exemple notvalidateAdresse : 211 TOWNE CENTER CIR, SANFORD, FL 32771.0 
     //   Console.WriteLine("Resources.ErrorNotParsable");
    //}

    //else
    //{
      //  var properties = result
        //    .GetType()
          //  .GetProperties()
            //.OrderBy(x => x.Name);
        //foreach (var property in properties)
        //{
          //  Console.WriteLine(
            //    "{0,30} : {1}",
              //  property.Name,
                //property.GetValue(result, null));
        //}
    //}

    //var readLine = Console.ReadLine() ?? string.Empty;
    //loop = readLine.ToUpperInvariant() == "Y";
//}
//while (loop);
//</summary >