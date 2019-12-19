using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Formatting = Newtonsoft.Json.Formatting;

#pragma warning disable 1591

namespace Frends.Csv
{

    public enum CreateInputType { List, Json }

    public class CreateInput
    {
        /// <summary>
        /// Select input type to show correct editor for input
        /// </summary>
        public CreateInputType InputType { get; set; }

        /// <summary>
        /// Json string to write to CSV. Must be an array of objects
        /// </summary>
        [DefaultValue("[{\"Column1\": \"row1Val1\",\"Column2\": \"row1Val2\"},{\"Column1\": \"row2Val1\",\"Column2\": \"row2Val2\"}]")]
        [UIHint(nameof(InputType), "", CreateInputType.Json)]
        [DisplayFormat(DataFormatString = "Json")]
        public string Json { get; set; }

        /// <summary>
        /// Headers for the data. Need to be in the same order as the underlying data
        /// </summary>
        [UIHint(nameof(InputType), "", CreateInputType.List)]
        public List<string> Headers { get; set; }

        /// <summary>
        /// Data to write to the csv string. Needs to be of type List&lt;List&lt;object&gt;&gt;. The order of the nested list objects need to be in the same order as the header list.
        /// </summary>
        [UIHint(nameof(InputType), "", CreateInputType.List)]
        [DisplayFormat(DataFormatString = "Expression")]
        public List<List<object>> Data { get; set; }

        [DefaultValue("\";\"")]
        public string Delimiter { get; set; }
    }

    public class CreateOption
    {
        /// <summary>
        /// This flag tells the writer if a header row should be written.
        /// </summary>
        [DefaultValue("true")]
        public bool IncludeHeaderRow { get; set; } = true;

        /// <summary>
        /// Specify the culture info to be used when creating csv. If this is left empty InvariantCulture will be used. List of cultures: https://msdn.microsoft.com/en-us/library/ee825488(v=cs.20).aspx Use the Language Culture Name.
        /// </summary>
        public string CultureInfo { get; set; } = "";

        /// <summary>
        /// If set true csv's fields are never put in quotes
        /// </summary>
        [DefaultValue("false")]
        public bool NeverAddQuotesAroundValues { get; set; }

        /// <summary>
        /// Input's null values will be replaced with this value
        /// </summary>
        [DisplayFormat(DataFormatString = "Text")]
        public string ReplaceNullsWith { get; set; }
    }

    public class CreateResult
    {
        public CreateResult(string csv)
        {
            Csv = csv;
        }

        public string Csv { get; }
    }

    public class ParseInput
    {
        /// <summary>
        /// Input csv string
        /// </summary>
        [DisplayFormat(DataFormatString = "Text")]
        public string Csv { get; set; }

        /// <summary>
        /// The separator used in the csv string
        /// </summary>
        [DefaultValue("\";\"")]
        public string Delimiter { get; set; }

        /// <summary>
        /// You can map columns to specific types. The order of the columns are used for mapping, that means that the ColumnSpecification elements need to be created in the same order as the CSV fields
        /// </summary>
        public ColumnSpecification[] ColumnSpecifications { get; set; }
    }

    public enum ColumnType { String, Int, Long, Decimal, Double, Boolean, DateTime, Char }

    public class ColumnSpecification
    {
        /// <summary>
        /// Name of the resulting column
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Type for the resulting column.
        /// </summary>
        public ColumnType Type { get; set; }
    }

    public class ParseOption
    {
        /// <summary>
        /// This flag tells the reader if there is a header row in the CSV string.
        /// </summary>
        [DefaultValue("true")]
        public bool ContainsHeaderRow { get; set; } = true;

        /// <summary>
        /// If the csv string contains metadata before the header row you can set this value to ignore a specific amount of rows from the beginning of the csv string.
        /// </summary>
        public int SkipRowsFromTop { get; set; }

        /// <summary>
        /// A flag to let the reader know if a record should be skipped when reading if it's empty. A record is considered empty if all fields are empty.
        /// </summary>
        [DefaultValue("true")]
        public bool SkipEmptyRows { get; set; } = true;

        /// <summary>
        /// This flag tells the reader to trim whitespace from the beginning and ending of the field value when reading.
        /// </summary>
        [DefaultValue("true")]
        public bool TrimOutput { get; set; } = true;

        /// <summary>
        ///  If intended header value contains whitespaces replace it(them) with this string, default action is to do nothing.
        /// </summary>
        [DisplayFormat(DataFormatString = "Text")]
        [DefaultValue(" ")]
        public string ReplaceHeaderWhitespaceWith { get; set; } = " ";

        /// <summary>
        /// The culture info to read/write the entries with, e.g. for decimal separators. InvariantCulture will be used by default. See list of cultures here: https://msdn.microsoft.com/en-us/library/ee825488(v=cs.20).aspx; use the Language Culture Name.
        /// NOTE: Due to an issue with the CsvHelpers library, all CSV tasks will use the culture info setting of the first CSV task in the process; you cannot use different cultures for reading and parsing CSV files in the same process.|
        /// </summary>
        public string CultureInfo { get; set; } = "";

    }

    public class ParseResult
    {
        private readonly Lazy<object> _jToken;
        private readonly Lazy<string> _xml;
        private static CultureInfo _culture;
        public ParseResult(List<List<object>> data, List<string> headers, CultureInfo configurationCultureInfo)
        {
            Data = data;
            Headers = headers;
            _culture = configurationCultureInfo;
            _jToken = new Lazy<object>(() => Data != null ? WriteJToken(data,headers) : null);
            _xml = new Lazy<string>(() => Data != null ? WriteXmlString(data, headers) : null);
        }

        private static string WriteXmlString(IEnumerable<List<object>> data, IReadOnlyList<string> headers)
        {
            using (var ms = new MemoryStream()) {
                using (var writer = new XmlTextWriter(ms, new UTF8Encoding(false)) {Formatting = System.Xml.Formatting.Indented})
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("Root");

                    foreach (var row in data)
                    {
                        writer.WriteStartElement("Row");

                        for (var i = 0; i < headers.Count; i++)
                        {
                            writer.WriteElementString(headers[i], Convert.ToString(row[i], _culture));
                        }
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                }
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        private static object WriteJToken(IEnumerable<List<object>> data, IReadOnlyList<string> headers)
        {
            using (var writer = new JTokenWriter())
            {
                writer.Formatting = Formatting.Indented;
                writer.Culture = _culture;
                writer.WriteStartArray();
                foreach (var row in data)
                {
                    writer.WriteStartObject();

                    for (var i = 0; i < headers.Count; i++)
                    {
                        writer.WritePropertyName(headers[i]);
                        writer.WriteValue(row[i]);
                    }
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                return writer.Token;
            }
        }

        public List<List<object>> Data { get; }
        public List<string> Headers { get; set; }
        public object ToJson()
        {
            return _jToken.Value;
        }
        public string ToXml()
        {
            return _xml.Value;
        }
    }
}
