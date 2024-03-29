﻿using CsvHelper;
using CsvHelper.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;

#pragma warning disable 1591

namespace Frends.Csv
{
    public class Csv
    {
        /// <summary>
        /// Parse string csv content to a object. See https://github.com/FrendsPlatform/Frends.Csv
        /// </summary>
        /// <returns>Object { List&lt;List&lt;object&gt;&gt; Data, List&lt;string&gt; Headers, JToken ToJson(), string ToXml() } </returns>
        public static ParseResult Parse([PropertyTab] ParseInput input, [PropertyTab] ParseOption option)
        {
            CsvConfiguration configuration;

            if (!option.IgnoreReferences && option.IgnoreQuotes)
            {
                configuration = new CsvConfiguration(new CultureInfo(option.CultureInfo))
                {
                    HasHeaderRecord = option.ContainsHeaderRow,
                    Delimiter = input.Delimiter,
                    TrimOptions = option.TrimOutput ? TrimOptions.Trim : TrimOptions.None,
                    IgnoreBlankLines = option.SkipEmptyRows,
                    IgnoreReferences = option.IgnoreReferences,
                    Mode = !option.IgnoreReferences && option.IgnoreQuotes ? CsvMode.NoEscape : CsvMode.Escape,
                };
            }
            else
            {
                configuration = new CsvConfiguration(new CultureInfo(option.CultureInfo))
                {
                    HasHeaderRecord = option.ContainsHeaderRow,
                    Delimiter = input.Delimiter,
                    TrimOptions = option.TrimOutput ? TrimOptions.Trim : TrimOptions.None,
                    IgnoreBlankLines = option.SkipEmptyRows,
                    IgnoreReferences = option.IgnoreReferences
                };
            }

            // Setting the MissingFieldFound -delegate property of configuration to null when
            // option.TreatMissingFieldsAsNulls is set to true for returning null values for missing fields.
            // Otherwise the default setting which throws a MissingFieldException is used.

            if (option.TreatMissingFieldsAsNulls)
                configuration.MissingFieldFound = null;

            using (TextReader sr = new StringReader(input.Csv))
            {
                // Read rows before passing textreader to csvreader for so that header row would be in the correct place.
                for (var i = 0; i < option.SkipRowsFromTop; i++)
                    sr.ReadLine();

                using (var csvReader = new CsvReader(sr, configuration))
                {
                    if (option.ContainsHeaderRow)
                    {
                        csvReader.Read();
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
                        if (string.Equals(option.ReplaceHeaderWhitespaceWith, " "))
                            headers = csvReader.HeaderRecord.ToList();
                        else
                            foreach (string header in csvReader.HeaderRecord)
                                if (string.IsNullOrEmpty(header) || string.IsNullOrWhiteSpace(header))
                                    headers.Add(option.ReplaceHeaderWhitespaceWith);
                                else
                                    headers.Add(header.Replace(" ", option.ReplaceHeaderWhitespaceWith));

                        while (csvReader.Read())
                        {
                            var innerList = new List<object>();
                            for (var index = 0; index < csvReader.HeaderRecord.Length; index++)
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
                            throw new ArgumentException("Csv input can not be empty");

                        for (int i = 0; i < csvReader.Parser.Record.Length; i++)
                            headers.Add($"Column{i + 1}");

                        resultData.Add(new List<object>(csvReader.Parser.Record));
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
        /// Create a csv string from object or from a json string. See https://github.com/FrendsPlatform/Frends.Csv
        /// </summary>
        /// <returns>Object { string Csv } </returns>
        public static CreateResult Create([PropertyTab] CreateInput input, [PropertyTab] CreateOption option)
        {
            var config = new CsvConfiguration(new CultureInfo(option.CultureInfo))
            {
                Delimiter = input.Delimiter,
                HasHeaderRecord = option.IncludeHeaderRow
            };

            if (option.ForceQuotesAroundValues)
                config.ShouldQuote = (field) => true;
            
            if (option.NeverAddQuotesAroundValues)
            {
                config.Mode = CsvMode.NoEscape;
                // If IgnoreQuotes is true, seems like ShouldQuote function has to return false in all cases.
                // If IgnoreQuotes is false ShouldQuote can't have any implementation otherwise it will overwrite IgnoreQuotes statement (might turn it on again).
                config.ShouldQuote = (field) => false;
            }

            var csv = string.Empty;

            switch (input.InputType)
            {
                case CreateInputType.List:
                    csv = ListToCsvString(input.Data, input.Headers, config, option);
                    break;

                case CreateInputType.Json:
                    csv = JsonToCsvString(input.Json, config, option);
                    break;
            }
            return new CreateResult(csv);
        }

        private static string ListToCsvString(List<List<object>> inputData, List<string> inputHeaders, CsvConfiguration config, CreateOption option)
        {
            using (var csvString = new StringWriter())
            using (var csv = new CsvWriter(csvString, config))
            {
                // Write the header row.
                if (config.HasHeaderRecord && inputData.Any())
                {
                    foreach (var column in inputHeaders)
                        csv.WriteField(column);
                    csv.NextRecord();
                }

                foreach (var row in inputData)
                {
                    foreach (var cell in row)
                        csv.WriteField(cell ?? option.ReplaceNullsWith);
                    csv.NextRecord();
                }
                return csvString.ToString();
            }
        }

        private static string JsonToCsvString(string json, CsvConfiguration config, CreateOption option)
        {
            List<Dictionary<string, string>> data = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(json);

            using (var csvString = new StringWriter())
            using (var csv = new CsvWriter(csvString, config))
            {
                // Write the header row.
                if (config.HasHeaderRecord && data.Any())
                {
                    foreach (var column in data.First().Keys)
                        csv.WriteField(column);
                    csv.NextRecord();
                }

                foreach (var row in data)
                {
                    foreach (var cell in row)
                        csv.WriteField(cell.Value ?? option.ReplaceNullsWith);
                    csv.NextRecord();
                }
                return csvString.ToString();
            }
        }
    }
}