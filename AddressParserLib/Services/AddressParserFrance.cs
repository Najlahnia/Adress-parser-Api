// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AddressParser.cs" company="Jim Counts">
//     Copyright (c) Jim Counts 2013.
// </copyright>
// <summary>
//   Defines the AddressParser type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AddressParserLib;
using AddressParserLib.Constants;
using AddressParserLib.Helpers;
using AddressParserLib.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace AddressParserLib
{
    /// <summary>
    ///     <para>
    ///         This is an attempt at a port of the Perl CPAN module Geo::StreetAddress::US
    ///         to C#. It's a regex-based street address and street intersection parser for the
    ///         United States.
    ///     </para>
    ///     <para>
    ///         The original Perl version was written and is copyrighted by
    ///         Schuyler D. Erle &lt;schuyler@geocoder.us&gt; and is accessible at
    ///         <a href="http://search.cpan.org/~timb/Geo-StreetAddress-US-1.03/US.pm">CPAN</a>.
    ///     </para>
    ///     <para>
    ///         It says that "this library is free software; you can redistribute it and/or modify
    ///         it under the same terms as Perl itself, either Perl version 5.8.4 or, at
    ///         your option, any later version of Perl 5 you may have available."
    ///     </para>
    ///     <para>
    ///         According to the <a href="http://dev.perl.org/licenses/">Perl licensing page</a>,
    ///         that seems to mean you have a choice between GPL V1 (or at your option, a later version)
    ///         or the Artistic License.
    ///     </para>
    /// </summary>
    public class AddressParserFrance : AdressParserAbstract
    {
        /// <summary>
        /// In the <see cref="M:addressRegex"/> member, these are the names
        /// of the groups in the result that we care to inspect.
        /// </summary>
        private readonly string[] _fields =
            {
                Components.Number, Components.Predirectional, Components.Street, Components.StreetLine, Components.Suffix,
                Components.Postdirectional, Components.City, Components.State, Components.Zip, Components.SecondaryUnit,
                Components.SecondaryNumber
            };

        /// <summary>
        /// A regular expression that only extracts the street address fields.
        /// </summary>
        private Regex _addressLineRegex;

        /// <summary>
        /// The gigantic regular expression that actually extracts the bits and pieces
        /// from a given address.
        /// </summary>
        private Regex _addressRegex;

        /// <summary>
        /// A combined dictionary of the ranged and not ranged secondary units.
        /// </summary>
        private Dictionary<string, string> _allSecondaryUnits;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddressParser"/> class.
        /// </summary>
        public AddressParserFrance()
        {
            CompileRegex = true;
        }

        /// <summary>
        /// Gets the default parser instance.
        /// </summary>
        /// <value>
        /// The default parser.
        /// </value>
        public static AddressParserFrance Default { get; } = new AddressParserFrance();

        /// <summary>
        /// The postal box pattern without the place fields (no city/state/zip)
        /// </summary>
        public static string PostalBoxPatternAddressLineOnly => @"(?<{0}>(P[\.\s]?O[\.\s]?\s?)?BOX\s[0-9]+)".FormatInvariant(Components.StreetLine);

        /// <summary>
        /// Gets the zip pattern.
        /// </summary>
        /// <value>
        /// The zip pattern.
        /// </value>
        public static string ZipPattern => @"\d{5}(?:-?\d{4})?";

        /// <summary>
        /// Gets the regular expression that only extracts the street address fields.
        /// </summary>
        public Regex AddressLineRegex => _addressLineRegex ?? (_addressLineRegex = InitializeAddressLineRegex());

        /// <summary>
        /// Gets the gigantic regular expression that actually extracts the bits and pieces
        /// from a given address.
        /// </summary>
        public Regex AddressRegex => _addressRegex ?? (_addressRegex = InitializeRegex());

        /// <summary>
        /// Gets the pattern to match all known secondary units.
        /// </summary>
        /// <value>
        /// The all secondary unit pattern.
        /// </value>
        public string AllSecondaryUnitPattern => @"
                    (
                        (:?
                            (?: (?:{0} \W*)
                                | (?<{2}>\#)\W*
                            )
                            (?<{3}>[\w-]+)
                        )
                        |{1}
                    ),?
                ".FormatInvariant(
            RangedSecondaryUnitPattern,
            RangelessSecondaryUnitPattern,
            Components.SecondaryUnit,
            Components.SecondaryNumber);

        /// <summary>
        /// Gets a combined dictionary of the ranged and not ranged secondary units.
        /// </summary>
        public Dictionary<string, string> AllUnits => _allSecondaryUnits ?? (_allSecondaryUnits = CombineSecondaryUnits());

        /// <summary>
        /// Gets the city and state pattern.
        /// </summary>
        /// <value>
        /// The city and state pattern.
        /// </value>
        public string CityAndStatePattern => @"
                    (?:
                        (?<{1}>[^\d,]+)\W+
                        (?<{2}>{0})
                    )
                ".FormatInvariant(
            StatePattern,
            Components.City,
            Components.State);

        /// <summary>
        /// Gets or sets a value indicating whether to compile the regular expression objects. Enabled by default.
        /// </summary>
        public bool CompileRegex { get; set; }

        /// <summary>
        /// Gets a map of directional names (north, northeast, etc.) to abbreviations (N, NE, etc.).
        /// </summary>
        public Dictionary<string, string> DirectionalNames { get; } = new Dictionary<string, string>
        {
           { "NORTH", "NORD" },
    { "NORTHEAST", "NORD-EST" },
    { "EAST", "EST" },
    { "SOUTHEAST", "SUD-EST" },
    { "SOUTH", "SUD" },
    { "SOUTHWEST", "SUD-OUEST" },
    { "WEST", "OUEST" },
    { "NORTHWEST", "NORD-OUEST" }
        };

        /// <summary>
        /// Gets the pattern to match direction indicators (north, south, east, west, etc.)
        /// </summary>
        /// <value>
        /// The directional pattern.
        /// </value>
        public string DirectionalPattern
        {
            get
            {
                var arguments = DirectionalNames.Values
                    .Select(x => Regex.Replace(x, @"(\w)", @"$1\."))
                    .Concat(DirectionalNames.Keys)
                    .Concat(DirectionalNames.Values)
                    .OrderByDescending(x => x.Length)
                    .Distinct();
                return string.Join("|", arguments);
            }
        }

        /// <summary>
        /// Gets the match options to use with the address regular expression.
        /// </summary>
        public RegexOptions MatchOptions
        {
            get
            {
                var options = RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace
                    | RegexOptions.IgnoreCase;

                if (CompileRegex)
                {
                    options |= RegexOptions.Compiled;
                }

                return options;
            }
        }

        /// <summary>
        /// Gets the pattern to match city, state and zip.
        /// </summary>
        /// <value>
        /// The place pattern.
        /// </value>
        public string PlacePattern => @"
                    (?:{0}\W*)?
                    (?:(?<{2}>{1}))?
                ".FormatInvariant(CityAndStatePattern, ZipPattern, Components.Zip);

        /// <summary>
        /// Gets the post office box pattern.
        /// </summary>
        /// <value>
        /// The postal box pattern.
        /// </value>
        public string PostalBoxPattern => @"# Special case for PO boxes
                    (
                        \W*
                        {0}\W+
                        {1}
                        \W*
                    )".FormatInvariant(PostalBoxPatternAddressLineOnly, PlacePattern);

        /// <summary>
        /// Gets the ranged secondary unit pattern.
        /// </summary>
        /// <value>
        /// The ranged secondary unit pattern.
        /// </value>
        public string RangedSecondaryUnitPattern
        {
            get
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    @"(?<{1}>{0})(?![a-z])",
                    string.Join("|", RangedUnits.Keys.OrderByDescending(x => x.Length)),
                    Components.SecondaryUnit);
            }
        }

        /// <summary>
        /// Gets a map from unit names that require a number after them to their standard forms.
        /// </summary>
        public Dictionary<string, string> RangedUnits { get; } = new Dictionary<string, string>
        {
            {
                @"SU?I?TE",
                "STE"
            },
            {
                @"(?:AP)(?:AR)?T(?:ME?NT)?",
                "APT"
            },
            {
                @"(?:DEP)(?:AR)?T(?:ME?NT)?",
                "DEPT"
            },
            {
                @"RO*M",
                "RM"
            },
            {
                @"FLO*R?",
                "FL"
            },
            {
                @"UNI?T",
                "UNIT"
            },
            {
                @"BU?I?LDI?N?G",
                "BLDG"
            },
            {
                @"HA?NGA?R",
                "HNGR"
            },
            {
                @"KEY",
                "KEY"
            },
            {
                @"LO?T",
                "LOT"
            },
            {
                @"PIER",
                "PIER"
            },
            {
                @"SLIP",
                "SLIP"
            },
            {
                @"SPA?CE?",
                "SPACE"
            },
            {
                @"STOP",
                "STOP"
            },
            {
                @"TRA?I?LE?R",
                "TRLR"
            },
            {
                @"BOX",
                "BOX"
            }
        };

        /// <summary>
        /// Gets the pattern to match unit names that do not require a number after them.
        /// </summary>
        /// <value>
        /// The unit name pattern for units that do not require a number.
        /// </value>
        public string RangelessSecondaryUnitPattern
        {
            get
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    @"\b(?<{1}>{0})\b",
                    string.Join("|", RangelessUnits.Keys.OrderByDescending(x => x.Length)),
                    Components.SecondaryUnit);
            }
        }

        /// <summary>
        /// Gets a map from unit names that do not require a number after them to their standard forms.
        /// </summary>
        public Dictionary<string, string> RangelessUnits { get; } = new Dictionary<string, string>
{
    {
        "BA?SE?ME?N?T",
        "SS"
    },
    {
        "FRO?NT",
        "FRNT"
    },
    {
        "LO?BBY",
        "LOBBY"
    },
    {
        "LOWE?R",
        "LOWER"
    },
    {
        "OFF?I?CE?",
        "BUREAU"
    },
    {
        "PE?N?T?HO?U?S?E?",
        "PH"
    },
    {
        "REAR",
        "ARR"
    },
    {
        "SIDE",
        "CÔTÉ"
    },
    {
        "UPPE?R",
        "SUP"
    }
};

        /// <summary>
        /// Gets the pattern to match states and provinces.
        /// </summary>
        /// <value>
        /// The state pattern.
        /// </value>
        public string StatePattern
        {
            get
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    @"\b(?:{0})\b?",
                    string.Join("|", StatesAndProvinces.Keys.Select(Regex.Escape).Concat(StatesAndProvinces.Values).OrderByDescending(k => k.Length).Distinct()));
            }
        }

        /// <summary>
        /// Gets a map from lowercase US state and territory names to their canonical two-letter
        /// postal abbreviations.
        /// </summary>
        public Dictionary<string, string> StatesAndProvinces { get; } = new Dictionary<string, string>
{
    { "AUVERGNE-RHONE-ALPES", "AUVERGNE-RHONE-ALPES" },
    { "BOURGOGNE-FRANCHE-COMTE", "BOURGOGNE-FRANCHE-COMTE" },
    { "BRETAGNE", "BRETAGNE" },
    { "CENTRE-VAL DE LOIRE", "CENTRE-VAL DE LOIRE" },
    { "CORSE", "CORSE" },
    { "GRAND EST", "GRAND EST" },
    { "HAUTS-DE-FRANCE", "HAUTS-DE-FRANCE" },
    { "ILE-DE-FRANCE", "ILE-DE-FRANCE" },
    { "NORMANDIE", "NORMANDIE" },
    { "NOUVELLE-AQUITAINE", "NOUVELLE-AQUITAINE" },
    { "OCCITANIE", "OCCITANIE" },
    { "PAYS DE LA LOIRE", "PAYS DE LA LOIRE" },
    { "PROVENCE-ALPES-COTE D'AZUR", "PROVENCE-ALPES-COTE D'AZUR" },
    { "AUDE", "AUDE" },
    { "AVEYRON", "AVEYRON" },
    { "BOUCHES-DU-RHONE", "BOUCHES-DU-RHONE" },
    { "CALVADOS", "CALVADOS" },
    { "CANTAL", "CANTAL" },
    { "CHARENTE", "CHARENTE" },
    { "CHARENTE-MARITIME", "CHARENTE-MARITIME" },
    { "CÔTE-D'OR", "CÔTE-D'OR" },
    { "CÔTES-D'ARMOR", "CÔTES-D'ARMOR" },
    { "CREUSE", "CREUSE" },
  
    { "DORDOGNE", "DORDOGNE" },
    { "DOUBS", "DOUBS" },
    { "DROME", "DROME" },
    { "EURE", "EURE" },
    { "EURE-ET-LOIR", "EURE-ET-LOIR" },
    { "FINISTÈRE", "FINISTÈRE" },
    { "GARD", "GARD" },
    { "GERS", "GERS" },
    { "GIRONDE", "GIRONDE" },
    { "HÉRAULT", "HÉRAULT" },
    { "ILLE-ET-VILAINE", "ILLE-ET-VILAINE" },
    { "INDRE", "INDRE" },
    { "INDRE-ET-LOIRE", "INDRE-ET-LOIRE" },
    { "ISÈRE", "ISÈRE" },
    { "JURA", "JURA" },
    { "LANDES", "LANDES" },
    { "LOIR-ET-CHER", "LOIR-ET-CHER" },
    { "LOIRE", "LOIRE" },
    { "HAUTE-LOIRE", "HAUTE-LOIRE" },
    { "LOIRE-ATLANTIQUE", "LOIRE-ATLANTIQUE" },
    { "LOIRET", "LOIRET" },
    { "LOT", "LOT" },
    { "LOT-ET-GARONNE", "LOT-ET-GARONNE" },
    { "LOZÈRE", "LOZÈRE" },
    { "MAINE-ET-LOIRE", "MAINE-ET-LOIRE" },
    { "MANCHE", "MANCHE" },
    { "MARNE", "MARNE" },
    { "HAUTE-MARNE", "HAUTE-MARNE" },
    { "MAYENNE", "MAYENNE" },
    { "MEURTHE-ET-MOSELLE", "MEURTHE-ET-MOSELLE" },
    { "MEUSE", "MEUSE" },
    { "MORBIHAN", "MORBIHAN" },
    { "MOSELLE", "MOSELLE" },
    { "NIEVRE", "NIEVRE" },
    { "NORD", "NORD" },
    { "OISE", "OISE" },
    { "ORNE", "ORNE" },
    { "PAS-DE-CALAIS", "PAS-DE-CALAIS" },
    { "PUY-DE-DÔME", "PUY-DE-DÔME" },
    { "PYRÉNÉES-ATLANTIQUES", "PYRÉNÉES-ATLANTIQUES" },
    { "HAUTES-PYRÉNÉES", "HAUTES-PYRÉNÉES" },
    { "PYRÉNÉES-ORIENTALES", "PYRÉNÉES-ORIENTALES" },
    { "BAS-RHIN", "BAS-RHIN" },
    { "HAUT-RHIN", "HAUT-RHIN" },
    { "RHÔNE", "RHÔNE" },
    { "HAUTE-SAÔNE", "HAUTE-SAÔNE" },
    { "SAÔNE-ET-LOIRE", "SAÔNE-ET-LOIRE" },
    { "SARTHE", "SARTHE" },
    { "SAVOIE", "SAVOIE" },
    { "HAUTE-SAVOIE", "HAUTE-SAVOIE" },
    { "SEINE-ET-MARNE", "SEINE-ET-MARNE" },
    { "YVELINES", "YVELINES" },
   
    { "SOMME", "SOMME" },
    { "TARN", "TARN" },
    { "TARN-ET-GARONNE", "TARN-ET-GARONNE" },
    { "VAR", "VAR" },
    { "VAUCLUSE", "VAUCLUSE" },
    { "VENDÉE", "VENDÉE" },
    { "VIENNE", "VIENNE" },
    { "HAUTE-VIENNE", "HAUTE-VIENNE" },
    { "VOSGES", "VOSGES" },
    { "YONNE", "YONNE" },
    { "TERRITOIRE DE BELFORT", "TERRITOIRE DE BELFORT" },
    { "ESSONNE", "ESSONNE" },
    { "HAUTS-DE-SEINE", "HAUTS-DE-SEINE" },
    { "SEINE-SAINT-DENIS", "SEINE-SAINT-DENIS" },
    { "VAL-DE-MARNE", "VAL-DE-MARNE" },
    { "VAL-D'OISE", "VAL-D'OISE" },
    { "GUADELOUPE", "GUADELOUPE" },
    { "MARTINIQUE", "MARTINIQUE" },
    { "GUYANE", "GUYANE" },
    { "RÉUNION", "RÉUNION" },
    { "MAYOTTE", "MAYOTTE" },
};


        /// <summary>
        /// Gets the pattern to match the street number, name, and suffix.
        /// </summary>
        /// <value>
        /// The street pattern.
        /// </value>
        public string StreetPattern => @"
                        (?:
                          # special case for addresses like 100 COUNTY ROAD F  http://regexstorm.net/tester?p=%28%3f%3a%28%3f%3cSTREET%3eCOUNTY%5cW*ROAD%5cW*%5cw%2b%29%29&i=COUNTY+ROAD+FG%0d%0a%0d%0a&o=ixs
                          (?:(?<STREET>COUNTY\W*ROAD\W*\w+))
                          |
                          # special case for addresses like 3419 Avenue C  http://regexstorm.net/tester?p=%28%3f%3a%28%3f%3cSTREET%3eAVENUE%5cW*%5cw%2b%29%29%0d%0a%23%28%3f%3a%28%3f%3cSTREET%3eAVENUE%5cW*%5ba-zA-Z%5d%7b1%7d%29%29%28%3f%3a%5b%5cs%5d%7c%5b%2c%5d%2b%29%0d%0a&i=3419+Avenue+C+Council+Bluffs+IA+51501%0d%0a5946+AVENUE+E%2c+MCINTOSH%2c+FL+32664%0d%0a&o=ixs
                          (?:(?<STREET>AVENUE\W*\w+))
                          |
                          # special case for addresses like 100 South Street
                          (?:(?<{2}>{0})\W+
                             (?<{3}>{1})\b)
                          |
                          (?:(?<{4}>{0})\W+)?
                          (?:
                            (?<{2}>[^,]*\d)
                            (?:[^\w,]*(?<{5}>{0})\b)
                           |
                            (?<{2}>[^,]+)
                            (?:[^\w,]+(?<{3}>{1})\b)
                            (?:[^\w,]+(?<{5}>{0})\b)?
                           |
                            (?<{2}>[^,]+?)
                            (?:[^\w,]+(?<{3}>{1})\b)?
                            (?:[^\w,]+(?<{5}>{0})\b)?
                          )
                        )
                    ".FormatInvariant(DirectionalPattern, SuffixPattern, Components.Street, Components.Suffix, Components.Predirectional, Components.Postdirectional);

        /// <summary>
        /// Gets a map from the lowercase USPS standard street suffixes to their canonical postal
        /// abbreviations as found in TIGER/Line.
        /// </summary>
        public Dictionary<string, string> StreetSuffixes { get; } = new Dictionary<string, string>
{
    { "ALLÉE", "ALLÉE" },
    { "ALLEY", "ALLEY" },
    { "ALLY", "ALLY" },
    { "ANNEXE", "ANNEXE" },
    { "ANNEX", "ANNEX" },
    { "ANNX", "ANNX" },
    { "ARCADE", "ARCADE" },
    { "AV", "AV" },
    { "AVEN", "AVEN" },
    { "AVENU", "AVENU" },
    { "AVENUE", "AVENUE" },
    { "AVN", "AVN" },
    { "AVNUE", "AVNUE" },
    { "BAYOO", "BAYOO" },
    { "BAYOU", "BAYOU" },
    { "PLAGE", "PLAGE" },
    { "BEND", "BEND" },
    { "BLUF", "BLUF" },
    { "BLUFF", "BLUFF" },
    { "BLUFFS", "BLUFFS" },
    { "BOT", "BOT" },
    { "BOTTM", "BOTTM" },
    { "BOTTOM", "BOTTOM" },
    { "BOUL", "BOUL" },
    { "BOULEVARD", "BOULEVARD" },
    { "BOULV", "BOULV" },
    { "BRANCHE", "BRANCHE" },
    { "BRIDGE", "BRIDGE" },
    { "BROOK", "BROOK" },
    { "BYPASS", "BYPASS" },
    { "CAMP", "CAMP" },
    { "CANYON", "CANYON" },
    { "CAPE", "CAPE" },
    { "CAUSEWAY", "CAUSEWAY" },
    { "CENTER", "CENTER" },
    { "CENTRE", "CENTRE" },
    { "CIRCLE", "CIRCLE" },
    { "CLIFF", "CLIFF" },
    { "CLIFFS", "CLIFFS" },
    { "CLUB", "CLUB" },
    { "COMMON", "COMMON" },
    { "CORNER", "CORNER" },
    { "CORNERS", "CORNERS" },
    { "COURSE", "COURSE" },
    { "COURT", "COURT" },
    { "COURTS", "COURTS" },
    { "COVE", "COVE" },
    { "CREEK", "CREEK" },
    { "CRESCENT", "CRESCENT" },
    { "CREST", "CREST" },
    { "CROSSING", "CROSSING" },
    { "CROSSROAD", "CROSSROAD" },
    { "CROSSROADS", "CROSSROADS" },
    { "CURVE", "CURVE" },
    { "DALE", "DALE" },
    { "DAM", "DAM" },
    { "DIVIDE", "DIVIDE" },
    { "DR", "DR" },
    { "DRIV", "DR" },
    { "DRIVE", "DRIVE" },
    { "DRV", "DR" },
    { "ESTATE", "ESTATE" },
    { "EST", "EST" },
    { "ESTATES", "ESTATES" },
    { "EXPRESSWAY", "EXPRESSWAY" },
    { "EXTENSION", "EXTENSION" },
    { "EXTN", "EXTENSION" },
    { "EXTNSN", "EXTENSION" },
    { "FALL", "FALL" },
    { "FALLS", "FALLS" },
    { "FERRY", "FERRY" },
    { "FIELD", "FIELD" },
    { "FIELDS", "FIELDS" },
    { "FLAT", "FLAT" },
    { "FLATS", "FLATS" },
    { "FORD", "FORD" },
    { "FORDS", "FORDS" },
    { "FOREST", "FOREST" },
    { "FORESTS", "FORESTS" },
    { "FORGE", "FORGE" },
    { "FORGES", "FORGES" },
    { "FORK", "FORK" },
    { "FORKS", "FORKS" },
    { "FORT", "FORT" },
    { "FREEWAY", "FREEWAY" },
    { "FRY", "FERRY" },
    { "GARDEN", "GARDEN" },
    { "GARDN", "GARDEN" },
    { "GDN", "GARDEN" },
    { "GATEWAY", "GATEWAY" },
    { "GATE", "GATE" },
    { "GATWAY", "GATEWAY" },
    { "GLEN", "GLEN" },
    { "GLENS", "GLENS" },
    { "GREEN", "GREEN" },
    { "GREENS", "GREENS" },
    { "GROV", "GROVE" },
    { "GROVE", "GROVE" },
    { "GRV", "GROVE" },
    { "HARB", "HARBOR" },
    { "HARBOR", "HARBOR" },
    { "HARBR", "HARBOR" },
    { "HAVEN", "HAVEN" },
    { "HTS", "HEIGHTS" },
    { "HILL", "HILL" },
    { "HILLS", "HILLS" },
    { "HLLW", "HOLLOW" },
    { "HOLLOW", "HOLLOW" },
    { "HOLW", "HOLLOW" },
    { "HRBOR", "HARBOR" },
    { "HWAY", "HIGHWAY" },
    { "INLET", "INLET" },
    { "IS", "ISLAND" },
    { "ISLAND", "ISLAND" },
    { "ISLND", "ISLAND" },
    { "ISS", "ISLAND" },
    { "ISLE", "ISLE" },
    { "JCT", "JUNCTION" },
    { "JCTION", "JUNCTION" },
    { "JCTN", "JUNCTION" },
    { "JUNCTION", "JUNCTION" },
    { "JUNCTN", "JUNCTION" },
    { "JUNCTON", "JUNCTION" },
    { "KEY", "KEY" },
    { "KY", "KEY" },
    { "KNL", "KNOLL" },
    { "KNOL", "KNOLL" },
    { "KNOLL", "KNOLL" },
    { "LAKE", "LAKE" },
    { "LAKES", "LAKES" },
    { "LAND", "LAND" },
    { "LANDING", "LANDING" },
    { "LNDG", "LANDING" },
    { "LN", "LANE" },
    { "LGT", "LIGHT" },
    { "LIGHT", "LIGHT" },
    { "LK", "LAKE" },
    { "LKS", "LAKES" },
    { "LDG", "LODGE" },
    { "LODG", "LODGE" },
    { "LODGE", "LODGE" },
    { "LOOP", "LOOP" },
    { "MALL", "MALL" },
    { "MANOR", "MANOR" },
    { "MANR", "MANOR" },
    { "MDW", "MEADOW" },
    { "MDWS", "MEADOWS" },
    { "MEDOWS", "MEADOWS" },
    { "MEADOW", "MEADOW" },
    { "MEADOWS", "MEADOWS" },
    { "MILL", "MILL" },
    { "MILLS", "MILLS" },
    { "MISSN", "MISSION" },
    { "MSSN", "MISSION" },
    { "MOTORWAY", "MOTORWAY" },
    { "MOUNT", "MOUNT" },
    { "MT", "MOUNT" },
    { "MNT", "MOUNT" },
    { "MOUNTAIN", "MOUNTAIN" },
    { "MTN", "MOUNTAIN" },
    { "NCK", "NECK" },
    { "NECK", "NECK" },
    { "ORCH", "ORCHARD" },
    { "ORCHARD", "ORCHARD" },
    { "OVAL", "OVAL" },
    { "OVL", "OVAL" },
    { "PARK", "PARK" },
    { "PRK", "PARK" },
    { "PARKS", "PARKS" },
    { "PARKWAY", "PARKWAY" },
    { "PKWAY", "PARKWAY" },
    { "PKY", "PARKWAY" },
    { "PARKWAYS", "PARKWAYS" },
    { "PARKWY", "PARKWAYS" },
    { "PASS", "PASS" },
    { "PASSAGE", "PASSAGE" },
    { "PATH", "PATH" },
    { "PATHS", "PATHS" },
    { "PIKE", "PIKE" },
    { "PIKES", "PIKES" },
    { "PINE", "PINE" },
    { "PINES", "PINES" },
    { "PNES", "PINES" },
    { "PL", "PLACE" },
    { "PLAIN", "PLAIN" },
    { "PLAINS", "PLAINS" },
    { "PLAZA", "PLAZA" },
    { "PLZ", "PLAZA" },
    { "POINT", "POINT" },
    { "PT", "POINT" },
    { "POINTS", "POINTS" },
    { "PORT", "PORT" },
    { "PRT", "PORT" },
    { "PORTS", "PORTS" },
    { "PR", "PRAIRIE" },
    { "RAD", "RADIAL" },
    { "RADIAL", "RADIAL" },
    { "RADIEL", "RADIAL" },
    { "RAMP", "RAMP" },
    { "RANCH", "RANCH" },
    { "RAPID", "RAPID" },
    { "RPD", "RAPID" },
    { "RAPIDS", "RAPIDS" },
    { "RPDS", "RAPIDS" },
    { "REST", "REST" },
    { "RIDGE", "RIDGE" },
    { "RDG", "RIDGE" },
    { "RIDGES", "RIDGES" },
    { "RIV", "RIVER" },
    { "RIVER", "RIVER" },
    { "RIVR", "RIVER" },
    { "RD", "ROAD" },
    { "ROAD", "ROAD" },
    { "RDS", "ROADS" },
    { "ROADS", "ROADS" },
    { "ROUTE", "ROUTE" },
    { "ROW", "ROW" },
    { "RUE", "RUE" },
    { "RUN", "RUN" },
    { "SHL", "SHOAL" },
    { "SHOAL", "SHOAL" },
    { "SHLS", "SHOALS" },
    { "SHOARS", "SHORES" },
    { "SKWY", "SKYWAY" },
    { "SPG", "SPRING" },
    { "SPNG", "SPRING" },
    { "SPRINGS", "SPRINGS" },
    { "SPUR", "SPUR" },
    { "SQ", "SQUARE" },
    { "SQR", "SQUARE" },
    { "SQRE", "SQUARE" },
    { "ST", "STREET" },
    { "STRAV", "STRAVENUE" },
    { "STRAVENUE", "STRAVENUE" },
    { "STREAM", "STREAM" },
    { "STREME", "STREAM" },
    { "STRM", "STREAM" },
    { "STRT", "STREET" },
    { "STREETS", "STREETS" },
    { "SUMIT", "SUMMIT" },
    { "SUMMIT", "SUMMIT" },
    { "TER", "TERRACE" },
    { "TERR", "TERRACE" },
    { "THROUGHWAY", "THROUGHWAY" },
    { "TPK", "TURNPIKE" },
    { "TPKE", "TURNPIKE" },
    { "TR", "TRAIL" },
    { "TRACE", "TRACE" },
    { "TRACES", "TRACES" },
    { "TRACK", "TRACK" },
    { "TRACKS", "TRACKS" },
    { "TRK", "TRACK" },
    { "TRKS", "TRACKS" },
    { "TRAIL", "TRAIL" },
    { "TRAILS", "TRAILS" },
    { "TRL", "TRAIL" },
    { "TRLS", "TRAILS" },
    { "TRNPK", "TURNPIKE" },
    { "TRPK", "TURNPIKE" },
    { "TUNEL", "TUNNEL" },
    { "TUNL", "TUNNEL" },
    { "TUNLS", "TUNNELS" },
    { "TUNNEL", "TUNNEL" },
    { "TUNNELS", "TUNNELS" },
    { "TUNNL", "TUNNEL" },
    { "TURNPK", "TURNPIKE" },
    { "UN", "UNION" },
    { "UNIONS", "UNIONS" },
    { "VALLEY", "VALLEY" },
    { "VALLY", "VALLEY" },
    { "VDCT", "VIADUCT" },
    { "VIADCT", "VIADUCT" },
    { "VIA", "VIADUCT" },
    { "VIEW", "VIEW" },
    { "VIEWS", "VIEWS" },
    { "VILL", "VILLAGE" },
    { "VILLAG", "VILLAGE" },
    { "VILLAGE", "VILLAGE" },
    { "VILLG", "VILLAGE" },
    { "VIS", "VISTA" },
    { "VIST", "VISTA" },
    { "VISTA", "VISTA" },
    { "VL", "VILLE" },
    { "VLY", "VALLEY" },
    { "VLYS", "VALLEYS" },
    { "VSTA", "VISTA" },
    { "VW", "VIEW" },
    { "VWS", "VIEWS" },
    { "WALK", "WALK" },
    { "WALL", "WALL" },
    { "WAY", "WAY" },
    { "WELL", "WELL" },
    { "WELLS", "WELLS" },
    { "WY", "WAY" }
};






        /// <summary>
        /// Gets the pattern to match standard street suffixes
        /// </summary>
        /// <value>
        /// The suffix pattern.
        /// </value>
        public string SuffixPattern
        {
            get
            {
                return string.Join("|", StreetSuffixes.Values.Concat(StreetSuffixes.Keys).OrderByDescending(k => k.Length).Distinct());
            }
        }

        /// <summary>
        /// Attempts to parse the given input as a US address.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <returns>
        /// The parsed address, or null if the address could not be parsed.
        /// </returns>
        public override AddressParseResult ParseAddress(string input)
        {
            return ParseAddress(input, true);
        }

        /// <summary>
        /// Attempts to parse the given input as a US address.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <param name="normalize">if set to <c>true</c> then normalize extracted fields.</param>
        /// <returns>
        /// The parsed address, or null if the address could not be parsed.
        /// </returns>
        public override AddressParseResult ParseAddress(string input, bool normalize)
        {
            return ParseAddress(input, AddressRegex, normalize);
        }

        /// <summary>
        /// Attempts to parse the given input as a US address.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <param name="pattern">The regex pattern to use for matching address fields</param>
        /// <param name="normalize">if set to <c>true</c> then normalize extracted fields.</param>
        /// <returns>
        /// The parsed address, or null if the address could not be parsed.
        /// </returns>
        public override AddressParseResult ParseAddress(string input, Regex pattern, bool normalize)
        {
            if (pattern == null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            if (normalize)
            {
                input = input.ToUpperInvariant();
            }

            var match = pattern.Match(input);
            if (!match.Success)
            {
                return null;
            }

            var extracted = GetApplicableFields(match, pattern);
            if (normalize)
            {
                extracted = Normalize(extracted);
            }

            return new AddressParseResult(extracted, match);
        }

        /// <summary>
        /// Attempts to parse the given input as the street line of a US address.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <param name="normalize">if set to <c>true</c> then normalize extracted fields.</param>
        /// <returns>
        /// The parsed address fields, or null if the address could not be parsed.
        /// </returns>
        public AddressParseResult ParseAddressLine(string input, bool normalize)
        {
            return ParseAddress(input, AddressLineRegex, normalize);
        }

        /// <summary>
        /// Given a dictionary that maps regular expressions to USPS abbreviations,
        /// this function finds the first entry whose regular expression matches the given
        /// input value and supplies the corresponding USPS abbreviation as its output. If
        /// no match is found, the original value is returned.
        /// </summary>
        /// <param name="map">The dictionary that maps regular expressions to USPS abbreviations.</param>
        /// <param name="input">The value to test against the regular expressions.</param>
        /// <returns>The correct USPS abbreviation, or the original value if no regular expression
        /// matched successfully.</returns>
        private static string GetNormalizedValueByRegexLookup(Dictionary<string, string> map, string input)
        {
            var output = input;

            foreach (var pair in map)
            {
                var pattern = pair.Key;
                if (Regex.IsMatch(input, pattern))
                {
                    output = pair.Value;
                    break;
                }
            }

            return output;
        }

        /// <summary>
        /// Given a dictionary that maps strings to USPS abbreviations,
        /// this function finds the first entry whose key matches the given
        /// input value and supplies the corresponding USPS abbreviation as its output. If
        /// no match is found, the original value is returned.
        /// </summary>
        /// <param name="map">The dictionary that maps strings to USPS abbreviations.</param>
        /// <param name="input">The value to search for in the list of strings.</param>
        /// <returns>The correct USPS abbreviation, or the original value if no string
        /// matched successfully.</returns>
        private static string GetNormalizedValueByStaticLookup(IDictionary<string, string> map, string input)
        {
            if (!map.TryGetValue(input, out string output))
            {
                output = input;
            }

            return output;
        }

        /// <summary>
        /// Build a combined dictionary of both the ranged and singular secondary units.
        /// </summary>
        /// <returns>A map of ranged and singular secondary unit names to their standard forms.</returns>
        /// <remarks>This is used by the Normalize() method to convert the unit into the USPS
        /// standardized form.</remarks>
        private Dictionary<string, string> CombineSecondaryUnits()
        {
            return new[] { RangedUnits, RangelessUnits }.SelectMany(x => x)
                .ToDictionary(y => y.Key, y => y.Value);
        }

        /// <summary>
        /// Given a successful <see cref="Match"/>, this method creates a dictionary
        /// consisting of the fields that we actually care to extract from the address.
        /// </summary>
        /// <param name="match">The successful <see cref="Match"/> instance.</param>
        /// <param name="regex">The parsing <see cref="Regex"/>.</param>
        /// <returns>A dictionary in which the keys are the name of the fields and the values
        /// are pulled from the input address.</returns>
        private Dictionary<string, string> GetApplicableFields(Match match, Regex regex)
        {
            var applicable = new Dictionary<string, string>();

            foreach (var field in regex.GetGroupNames())
            {
                if (!_fields.Contains(field))
                {
                    continue;
                }

                var mg = match.Groups[field];
                if (mg != null && mg.Success)
                {
                    applicable[field] = mg.Value;
                }
            }

            return applicable;
        }

        /// <summary>
        /// Given a field type and an input value, this method returns the proper USPS
        /// abbreviation for it (or the original value if no substitution can be found or is
        /// necessary).
        /// </summary>
        /// <param name="field">The type of the field.</param>
        /// <param name="input">The value of the field.</param>
        /// <returns>The normalized value.</returns>
        private string GetNormalizedValueForField(string field, string input)
        {
            if (input == null)
            {
                return null;
            }

            var output = input;

            switch (field)
            {
                case Components.Predirectional:
                case Components.Postdirectional:
                    output = GetNormalizedValueByStaticLookup(DirectionalNames, input);
                    break;

                case Components.Suffix:
                    output = GetNormalizedValueByStaticLookup(StreetSuffixes, input);
                    break;

                case Components.SecondaryUnit:
                    output = GetNormalizedValueByRegexLookup(AllUnits, input);
                    break;

                case Components.State:
                    output = GetNormalizedValueByStaticLookup(StatesAndProvinces, input);
                    break;

                case Components.Number:
                    if (!input.Contains('/'))
                    {
                        output = input.Replace(" ", string.Empty);
                    }

                    break;
            }

            return output;
        }

        /// <summary>
        /// Builds the regular expression stored in <see cref="AddressLineRegex"/>
        /// that does the parsing for address line fields only.
        /// </summary>
        /// <returns>The parser <see cref="Regex"/>.</returns>
        private Regex InitializeAddressLineRegex()
        {
            var numberPattern = @"(
                    ((?<{0}>\d+)(?<{1}>(-[0-9])|(\-?[A-Z]))(?=\b))    # Unit-attached
                    |(?<{0}>\d+[\-\ ]?\d+\/\d+)                                   # Fractional
                    |(?<{0}>\d+-?\d*)                                             # Normal Number
                    |(?<{0}>[NSWE]\ ?\d+\ ?[NSWE]\ ?\d+)                          # Wisconsin/Illinois
                  )".FormatInvariant(Components.Number, Components.SecondaryNumber);

            var generalPattern = @"(
                        [^\w\#]*    # skip non-word chars except # (e.g. unit)
                        (  {0} )\W*
                           {1}\W*
                        (?:{2}\W+)?
                    )".FormatInvariant(numberPattern, StreetPattern, AllSecondaryUnitPattern);

            var addressPattern = @"
                    ^
                    {0}
                    |
                    {1}
                ".FormatInvariant(PostalBoxPatternAddressLineOnly, generalPattern);

            return new Regex(addressPattern, MatchOptions);
        }

        /// <summary>
        /// Builds the gigantic regular expression stored in the addressRegex static
        /// member that actually does the parsing.
        /// </summary>
        /// <returns>The parser <see cref="Regex"/>.</returns>
        private Regex InitializeRegex()
        {
            var numberPattern = @"(
                    ((?<{0}>\d+)(?<{1}>(-[0-9])|(\-?[A-Z]))(?=\b))    # Unit-attached
                    |(?<{0}>\d+[\-\ ]?\d+\/\d+)                                   # Fractional
                    |(?<{0}>\d+-?\d*)                                             # Normal Number
                    |(?<{0}>[NSWE]\ ?\d+\ ?[NSWE]\ ?\d+)                          # Wisconsin/Illinois
                  )".FormatInvariant(Components.Number, Components.SecondaryNumber);

            var armedForcesPattern = @"# Special case for APO/FPO/DPO addresses
                    (
                        [^\w\#]*
                        (?<{1}>.+?)
                        (?<{2}>[AFD]PO)\W+
                        (?<{3}>A[AEP])\W+
                        (?<{4}>{0})
                        \W*
                    )".FormatInvariant(ZipPattern, Components.StreetLine, Components.City, Components.State, Components.Zip);

            var generalPattern = @"(
                        [^\w\#]*    # skip non-word chars except # (e.g. unit)
                        (  {0} )\W*
                           {1}\W+
                        (?:{2}\W+)?
                           {3}
                        \W*         # require on non-word chars at end
                    )".FormatInvariant(numberPattern, StreetPattern, AllSecondaryUnitPattern, PlacePattern);

            var addressPattern = @"
                    ^
                    {0}
                    |
                    {1}
                    |
                    {2}
                    $           # right up to end of string
                ".FormatInvariant(armedForcesPattern, PostalBoxPattern, generalPattern);

            return new Regex(addressPattern, MatchOptions);
        }

        /// <summary>
        /// Given a set of fields pulled from a successful match, this normalizes each value
        /// by stripping off some punctuation and, if applicable, converting it to a standard
        /// USPS abbreviation.
        /// </summary>
        /// <param name="extracted">The dictionary of extracted fields.</param>
        /// <returns>A dictionary of the extracted fields with normalized values.</returns>
        private Dictionary<string, string> Normalize(IDictionary<string, string> extracted)
        {
            var normalized = new Dictionary<string, string>();

            foreach (var pair in extracted)
            {
                var key = pair.Key;
                var value = pair.Value;
                if (value == null)
                {
                    continue;
                }

                // Strip off some punctuation
                value = Regex.Replace(value, @"^\s+|\s+$|[^\/\w\s\-\#\&]", string.Empty);

                // Normalize to official abbreviations where appropriate
                value = GetNormalizedValueForField(key, value);

                normalized[key] = value;
            }

            // Special case for an attached unit
            if (extracted.ContainsKey(Components.SecondaryNumber)
                && (!extracted.ContainsKey(Components.SecondaryUnit) || string.IsNullOrWhiteSpace(extracted[Components.SecondaryUnit])))
            {
                normalized[Components.SecondaryUnit] = "APT";
            }

            return normalized;
        }


    }
}