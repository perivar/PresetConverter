using System.Text.RegularExpressions;
using CommonUtils;

namespace PresetConverterProject.NIKontaktNKS
{
    public static class SNPID_CSVParsers
    {
        public static void GenerateSNPIDList(string nksSettingsPath)
        {
            // Read settings into NKSLibraries.Libraries
            NKS.NksReadLibrariesInfo(nksSettingsPath, false, false);
            var libList = NKSLibraries.Libraries.Values.AsEnumerable();

            // find current directory
            var curDirPath = Directory.GetCurrentDirectory();

            // Read CSV
            var cvsPath = Path.Combine(curDirPath, "PresetConverterProject/NIKontaktNKS/SNPID List.csv");
            var csvList = IOUtils.ReadCSV(cvsPath, true, SnpidCSVParser, null, ";", false).Cast<NksLibraryDesc>();

            // Read CSV2
            // var cvs2Path = Path.Combine(curDirPath, "PresetConverterProject/NIKontaktNKS/SNPID List2.csv");
            // var csv2List = IOUtils.ReadCSV(cvs2Path, true, SnpidCSVParser, null, ";", false).Cast<NksLibraryDesc>();

            // Read CSV Code
            var cvsCodePath = Path.Combine(curDirPath, "PresetConverterProject/NIKontaktNKS/SNPID List Code.csv");

            // read RT_STRINGS_COMPANY as lookup list
            var rtCompanyPath = Path.Combine(curDirPath, "PresetConverterProject/NIKontaktNKS/RT_STRINGS_COMPANY.TXT");
            Dictionary<int, string> companyDict =
                File.ReadLines(rtCompanyPath).
                Select((value, number) => (value, number)).
                ToDictionary(x => x.number, x => x.value.Trim());

            // read RT_STRINGS with / delimiter
            var rtPath = Path.Combine(curDirPath, "PresetConverterProject/NIKontaktNKS/RT_STRINGS.TXT");
            var rtList = IOUtils.ReadCSV(rtPath, false, RTStringsParser, companyDict, "/", false).Cast<NksLibraryDesc>();
            var rtListWithId = rtList.Select(x =>
             new NksLibraryDesc
             {
                 Id = NKS.ConvertToBase36(long.Parse(x.Id)).PadLeft(3, '0'),
                 Company = x.Company,
                 Name = x.Name,
                 GenKey = x.GenKey
             });

            // check for differences in the csv files
            // var inCSVButNotCSV2 = csvList.Where(csv => csv2List.All(csv2 => csv2.Id != csv.Id));
            // var inCSV2ButNotCSV = csv2List.Where(csv2 => csvList.All(csv => csv.Id != csv2.Id));
            // var inBothCSVButDifferentName = csv2List.Join(csvList, csv2 => csv2.Id, csv => csv.Id, (csv2, csv) => new { csv2, csv }).Where(both => both.csv2.Name != both.csv.Name);

            // check for differences between rtlist and csv2List
            var inCSVButNotRT = csvList.Where(csv => rtListWithId.All(rt => rt.Id != csv.Id));
            var inRTButNotCSV = rtListWithId.Where(rt => csvList.All(csv => csv.Id != rt.Id));
            var inBothButDifferentName = rtListWithId.Join(csvList, rt => rt.Id, csv => csv.Id, (rt, csv) => new { rt, csv }).Where(both => both.rt.Name != both.csv.Name);

            // check agains the lib list (Settings.cfg)           
            var sameIDsButDifferentGenKeys = NKSLibraries.Libraries.Values.Join(rtListWithId, nks => nks.Id, rt => rt.Id, (nks, rt) => new { nks, rt }).Where(both => both.nks.GenKey != both.rt.GenKey);
            var inRTButNotLib = rtListWithId.Where(rt => NKSLibraries.Libraries.Values.All(nks => nks.Id != rt.Id));
            var inLibButNotRT = NKSLibraries.Libraries.Values.Where(nks => rtListWithId.All(rt => rt.Id != nks.Id));

            // build and save the complete list
            // var completeList = libList.Union(rtListWithId).Union(csvList).OrderBy(a => a.Id, new SemiNumericComparer());
            // var completeList = libList.Union(rtListWithId).OrderBy(a => a.Id, new SemiNumericComparer());
            var completeList = libList.Union(rtListWithId).Union(csvList).OrderBy(a => a.Id, new SemiNumericComparer());
            // var completeList = csvList.Union(csv2List).OrderBy(a => a.Id, new SemiNumericComparer());
            // var completeList = csv2List.OrderBy(a => a.Id, new SemiNumericComparer());
            // var completeList = libList.OrderBy(a => a.Id, new SemiNumericComparer());
            List<object> lines = completeList.Cast<object>().ToList();
            IOUtils.WriteCSV(cvsCodePath, lines, NKSLibCSVFormatter, NKSLibCSVHeader, ";");
            IOUtils.WriteCSV(cvsPath, lines, NKSLibCSVFormatterShort, NKSLibCSVHeaderShort, ";");
        }

        public static object SnpidCSVParser(int lineCounter, string[] splittedLine, Dictionary<int, string>? lookupDictionary)
        {
            var snpid = splittedLine[0];
            var companyName = splittedLine[1];
            var libraryName = splittedLine[2];

            NksLibraryDesc libDesc = new()
            {
                Id = snpid,
                Company = companyName,
                Name = libraryName
            };

            return libDesc;
        }

        public static object RTStringsParser(int lineCounter, string[] splittedLine, Dictionary<int, string>? lookupDictionary)
        {
            var snpidAndCode = splittedLine[0];

            Regex snpidAndCodeRegex = new(@"^(\d+):\s(.*?)$");
            Match match = snpidAndCodeRegex.Match(snpidAndCode);
            string? snpid = null;
            string? jdx = null;
            if (match.Success)
            {
                int id = int.Parse(match.Groups[1].Value);
                snpid = string.Format("{0:000}", id);
                jdx = match.Groups[2].Value;
            }

            var hu = splittedLine[1];

            NksLibraryDesc libDesc = new()
            {
                Id = snpid
            };
            NKS.NksGeneratingKeySetKeyStr(libDesc.GenKey, jdx);
            NKS.NksGeneratingKeySetIvStr(libDesc.GenKey, hu);

            var libraryName = splittedLine[2];
            var libraryCompanyId = splittedLine.Length > 3 ? splittedLine[3] : null;

            if (!String.IsNullOrEmpty(libraryName))
            {
                libDesc.Name = libraryName;
            }

            if (libraryCompanyId != null)
            {
                var companyId = int.Parse(libraryCompanyId);
                libDesc.Company = lookupDictionary[companyId];
            }

            return libDesc;
        }

        public static string NKSLibCSVHeader(string columnSeparator)
        {
            var elements = new List<string>
            {
                // "Line",
                "SNPID",
                "Company Name",
                "Product Name",
                "JDX",
                "HU"
            };

            return string.Join(columnSeparator, elements);
        }

        public static string NKSLibCSVFormatter(object line, int lineCounter, string columnSeparator)
        {
            var elements = new List<string>();
            var libDesc = (NksLibraryDesc)line;

            // elements.Add(String.Format("{0,4}", lineCounter));
            elements.Add(libDesc.Id);
            elements.Add(libDesc.Company);
            elements.Add(libDesc.Name);

            if (libDesc.GenKey.KeyLength > 0) elements.Add(StringUtils.ByteArrayToHexString(libDesc.GenKey.Key));
            if (libDesc.GenKey.IVLength > 0) elements.Add(StringUtils.ByteArrayToHexString(libDesc.GenKey.IV));

            return string.Join(columnSeparator, elements);
        }

        public static string NKSLibCSVHeaderShort(string columnSeparator)
        {
            var elements = new List<string>
            {
                // "Line",
                "SNPID",
                "Company Name",
                "Product Name"
            };

            return string.Join(columnSeparator, elements);
        }

        public static string NKSLibCSVFormatterShort(object line, int lineCounter, string columnSeparator)
        {
            var elements = new List<string>();
            var libDesc = (NksLibraryDesc)line;

            // elements.Add(String.Format("{0,4}", lineCounter));
            elements.Add(libDesc.Id);
            elements.Add(libDesc.Company ?? "Undefined");
            elements.Add(libDesc.Name ?? "Undefined");

            return string.Join(columnSeparator, elements);
        }
    }
}