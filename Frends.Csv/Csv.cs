using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using Frends.Tasks.Attributes;
using Newtonsoft.Json;

#pragma warning disable 1591

namespace Frends.Csv
{
    public class Csv
    {
        /// <summary>
        /// Parse string csv content to a object.
        /// </summary>
        /// <returns>Object { List&lt;List&lt;object&gt;&gt; Data, List&lt;string&gt; Headers, JToken ToJson(), string ToXml() } </returns>
        public static ParseResult Parse([CustomDisplay(DisplayOption.Tab)] ParseInput input, [CustomDisplay(DisplayOption.Tab)] ParseOption option)
        {
            var configuration = new CsvConfiguration
            {
                HasHeaderRecord = option.ContainsHeaderRow,
                Delimiter = input.Delimiter,
                TrimFields = option.TrimOutput,
                SkipEmptyRecords = option.SkipEmptyRows,
                CultureInfo = new CultureInfo(option.CultureInfo)
            };

            using (TextReader sr = new StringReader(input.Csv))
            {
                //Read rows before passing textreader to csvreader for so that header row would be in the correct place
                for (var i = 0; i < option.SkipRowsFromTop; i++)
                {
                    sr.ReadLine();
                }

                using (var csvReader = new CsvReader(sr, configuration))
                {
                    if (option.ContainsHeaderRow)
                    {
                       // csvReader.Read(); // this is needed for CsvHelper 3.0
                       csvReader.ReadHeader();
                    }
                    var resultData = new List<List<object>>();
                    var headers = new List<string>();

                    if (input.ColumnSpecifications.Any())
                    {
                        var typeList = new List<Type>();

                        foreach (var columnSpec in input.ColumnSpecifications)
                        {
                            typeList.Add(columnSpec.Type.ToType());
                            headers.Add(columnSpec.Name);
                        }

                        while (csvReader.Read())
                        {
                            var innerList = new List<object>();
                            for (var index = 0; index < input.ColumnSpecifications.Length; index++)
                            {
                                var obj = csvReader.GetField(typeList[index], index);
                                innerList.Add(obj);
                            }
                            resultData.Add(innerList);
                        }
                    }
                    else if (option.ContainsHeaderRow && !input.ColumnSpecifications.Any())
                    {
                        headers = csvReader.FieldHeaders.ToList();
                        while (csvReader.Read())
                        {
                            var innerList = new List<object>();
                            for (var index = 0; index < csvReader.FieldHeaders.Length; index++)
                            {
                                var obj = csvReader.GetField(index);
                                innerList.Add(obj);
                            }
                            resultData.Add(innerList);
                        }
                    }
                    else if (!option.ContainsHeaderRow && !input.ColumnSpecifications.Any())
                    {
                        if (!csvReader.Read())
                        {
                            throw new ArgumentException("Csv input can not be empty");
                        }

                        headers = csvReader.CurrentRecord.Select((x, index) => index.ToString()).ToList();
                        resultData.Add(new List<object>(csvReader.CurrentRecord));
                        while (csvReader.Read())
                        {
                            var innerList = new List<object>();
                            for (var index = 0; index < headers.Count; index++)
                            {
                                var obj = csvReader.GetField(index);
                                innerList.Add(obj);
                            }
                            resultData.Add(innerList);
                        }
                    }

                    return new ParseResult(resultData, headers, configuration.CultureInfo);

                }
            }
        }

        /// <summary>
        /// Create a csv string from object or from a json string.
        /// </summary>
        /// <returns>Object { string Csv } </returns>
        public static CreateResult Create([CustomDisplay(DisplayOption.Tab)] CreateInput input, [CustomDisplay(DisplayOption.Tab)] CreateOption option)
        {
            var config = new CsvConfiguration()
            {
                Delimiter = input.Delimiter,
                HasHeaderRecord = option.IncludeHeaderRow,
                CultureInfo = new CultureInfo(option.CultureInfo)
            };
            var csv = "";

            switch (input.InputType)
            {
                case CreateInputType.List:
                    csv = ListToCsvString(input.Data, input.Headers, config);
                    break;
                case CreateInputType.Json:
                    csv = JsonToCsvString(input.Json, config);
                    break;
            }
            return new CreateResult(csv);

        }

        private static string ListToCsvString(List<List<object>> inputData, List<string> inputHeaders, CsvConfiguration config)
        {

            using (var csvString = new StringWriter())
            using (var csv = new CsvWriter(csvString, config))
            {
                //Write the header row
                if (config.HasHeaderRecord && inputData.Any())
                {
                    foreach (var column in inputHeaders)
                    {
                        csv.WriteField(column);
                    }
                    csv.NextRecord();
                }

                foreach (var row in inputData)
                {
                    foreach (var cell in row)
                    {
                        csv.WriteField(cell);
                    }
                    csv.NextRecord();
                }
                return csvString.ToString();
            }
        }


        private static string JsonToCsvString(string json, CsvConfiguration config)
        {
            var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);

            using (var csvString = new StringWriter())
            using (var csv = new CsvWriter(csvString, config))
            {
                //Write the header row
                if (config.HasHeaderRecord && data.Any())
                {
                    foreach (var column in data.First().Keys)
                    {
                        csv.WriteField(column);
                    }
                    csv.NextRecord();
                }

                foreach (var row in data)
                {
                    foreach (var cell in row)
                    {
                        csv.WriteField(cell.Value);
                    }
                    csv.NextRecord();
                }
                return csvString.ToString();
            }
        }
    }
}
