using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Frends.Csv.Tests
{
    [TestFixture]
    public class Tests
    {
        [Test]
        public void TestParseSkipRowsWithAutomaticHeaders()
        {
            var csv = $@"asdasd
Coolio
year;car;mark;price
1997;Ford;E350;2,34
2000;Mercury;Cougar;2,38";
            var result = Csv.Parse(new ParseInput()
            {
                ColumnSpecifications = new ColumnSpecification[0],
                Delimiter = ";",
                Csv = csv
            }, new ParseOption() { ContainsHeaderRow = true, SkipRowsFromTop = 2, SkipEmptyRows = false });

            dynamic resultJArray = result.ToJson();
            var resultXml = result.ToXml();
            var resultData = result.Data;
            Assert.AreEqual(resultData.Count, 2);
            Assert.AreEqual(resultJArray.Count, 2);
            Assert.IsTrue(resultXml.Contains("<year>2000</year>"));
            Assert.AreEqual(resultJArray[0].price.ToString(), "2,34");
        }

        [Test]
        public void TestParseWithColumnSpecAndMissingHeader()
        {
            var csv = @"1997;Ford;E350;2,34
2000;Mercury;Cougar;2,38";

            var result = Csv.Parse(new ParseInput()
            {
                ColumnSpecifications = new[]
                {
                    new ColumnSpecification() {Name = "Year", Type = ColumnType.Int},
                    new ColumnSpecification() {Name = "Car", Type = ColumnType.String},
                    new ColumnSpecification() {Name = "Mark", Type = ColumnType.String},
                    new ColumnSpecification() {Name = "Price", Type = ColumnType.Decimal}
                },
                Delimiter = ";",
                Csv = csv
            }, new ParseOption() { ContainsHeaderRow = false, CultureInfo = "fi-FI" });
            var resultJArray = result.ToJson() as JArray;
            var resultXml = result.ToXml();
            var resultData = result.Data;
            Assert.AreEqual(resultData.Count, 2);
            Assert.AreEqual(resultJArray.Count, 2);
            Assert.IsTrue(resultXml.Contains("<Year>2000</Year>"));
            Assert.AreEqual(resultJArray[0]["Price"].Value<decimal>(), 2.34);
        }

        [Test]
        public void TestParseWithNoColumnSpecAndNoHeader()
        {
            var csv = @"1997;Ford;E350;2,34
2000;Mercury;Cougar;2,38";

            var result = Csv.Parse(new ParseInput()
            {
                ColumnSpecifications = new ColumnSpecification[0],
                Delimiter = ";",
                Csv = csv
            }, new ParseOption() { ContainsHeaderRow = false, CultureInfo = "fi-FI" });

            var resultJArray = result.ToJson() as JArray;
            var resultXml = result.ToXml();
            var resultData = result.Data;
            Assert.AreEqual(resultData.Count, 2);
            Assert.AreEqual(resultJArray.Count, 2);
            Assert.IsTrue(resultXml.Contains("<0>2000</0>"));
            Assert.AreEqual(resultJArray[0]["3"].Value<string>(), "2,34");
        }

        [Test]
        public void TestParseWillAllKindOfDataTypes()
        {
            var csv =
@"THIS;is;header;row;with;some;random;stuff ;yes
1997;""Fo;rd"";2,34;true;1;4294967296;f;2008-09-15;2008-05-01 7:34:42Z
2000;Mercury;2,38;false;0;4294967296;g;2009-09-15T06:30:41.7752486;Thu, 01 May 2008 07:34:42 GMT";
            var result = Csv.Parse(new ParseInput()
            {
                ColumnSpecifications = new[]
                {
                    new ColumnSpecification() {Name = "Int", Type = ColumnType.Int},
                    new ColumnSpecification() {Name = "String", Type = ColumnType.String},
                    new ColumnSpecification() {Name = "Decimal", Type = ColumnType.Decimal},
                    new ColumnSpecification() {Name = "Bool", Type = ColumnType.Boolean},
                    new ColumnSpecification() {Name = "Bool2", Type = ColumnType.Boolean},
                    new ColumnSpecification() {Name = "Long", Type = ColumnType.Long},
                    new ColumnSpecification() {Name = "Char", Type = ColumnType.Char},
                    new ColumnSpecification() {Name = "DateTime", Type = ColumnType.DateTime},
                    new ColumnSpecification() {Name = "DateTime2", Type = ColumnType.DateTime},
                },
                Delimiter = ";",
                Csv = csv
            }, new ParseOption() { ContainsHeaderRow = true, CultureInfo = "fi-FI", IgnoreReferences = true });
            var resultJson = (JArray) result.ToJson();
            Assert.AreEqual(resultJson[0]["Long"].Value<long>(), 4294967296);
            var resultXml = result.ToXml();
            Assert.IsTrue(resultXml.Contains("<DateTime2>1.5.2008 10.34.42</DateTime2>"));
            var resultData = result.Data;
            var itemArray = resultData[0];
            Assert.AreEqual(itemArray[0].GetType(), typeof(int));
            Assert.AreEqual(itemArray[0], 1997);

            Assert.AreEqual(itemArray[1].GetType(), typeof(string));
            Assert.AreEqual(itemArray[1], "Fo;rd");

            Assert.AreEqual(itemArray[2].GetType(), typeof(decimal));
            Assert.AreEqual(itemArray[2], 2.34d);

            Assert.AreEqual(itemArray[3].GetType(), typeof(bool));
            Assert.AreEqual(itemArray[3], true);

            Assert.AreEqual(itemArray[4].GetType(), typeof(bool));
            Assert.AreEqual(itemArray[4], true);

            Assert.AreEqual(itemArray[5].GetType(), typeof(long));
            Assert.AreEqual(itemArray[5], 4294967296);

            Assert.AreEqual(itemArray[6].GetType(), typeof(char));
            Assert.AreEqual(itemArray[6], 'f');

            Assert.AreEqual(itemArray[7].GetType(), typeof(DateTime));
            Assert.AreEqual(itemArray[7], new DateTime(2008, 9, 15));

            Assert.That(itemArray[8].GetType(), Is.EqualTo(typeof(DateTime)));
            Assert.That(itemArray[8], Is.EqualTo(new DateTime(2008, 5, 1, 10, 34, 42)));
        }

        [Test]
        public void TestParseTreatMissingFieldsAsNullSetToTrue()
        {
            var csv =
                @"header1,header2,header3
                value1,value2,value3
                value1,value2,value3
                value1,value2";

            var result = Csv.Parse(new ParseInput()
            {
                ColumnSpecifications = new ColumnSpecification[0],
                Delimiter = ",",
                Csv = csv
            }, new ParseOption() { ContainsHeaderRow = true, CultureInfo = "fi-FI", TreatMissingFieldsAsNulls = true });
            var resultJson = (JArray)result.ToJson();
            Assert.AreEqual(resultJson[2].Value<string>("header3"), null);

            var resultXml = result.ToXml();
            Assert.IsTrue(resultXml.Contains("<header3 />"));

            var resultData = result.Data;
            var nullItem = resultData[2][2];

            Assert.AreEqual(nullItem, null);
        }

        [Test]
        public void TestParseTreatMissingFieldsAsNullSetToFalse()
        {
            Assert.Throws<CsvHelper.MissingFieldException>(
                delegate
                {
                    var csv =
                      @"header1,header2,header3
                        value1,value2,value3
                        value1,value2,value3
                        value1,value2";

                    var result = Csv.Parse(new ParseInput()
                    {
                        ColumnSpecifications = new ColumnSpecification[0],
                        Delimiter = ",",
                        Csv = csv
                    }, new ParseOption() { ContainsHeaderRow = true, CultureInfo = "fi-FI", TreatMissingFieldsAsNulls = false });
                });
        }

        [Test]
        public void TestParseTreatMissingFieldsAsNullDefaultValue()
        {
            var options = new ParseOption();

            Assert.AreEqual(options.TreatMissingFieldsAsNulls, false);
        }

        [Test]
        public void TestWriteFromListTable()
        {
            var date = new DateTime(2000, 1, 1);
            var headers = new List<string>()
            {
                "Dosage",
                "Drug",
                "Patient",
                "Date"
            };
            var data = new List<List<object>>()
            {
                new List<object>() {25, "Indocin", "David", date},
                new List<object>() {50, "Enebrel", "Sam", date},
                new List<object>() {10, "Hydralazine", "Christoff", date},
                new List<object>() {21, "Combiv;ent", "Janet", date},
                new List<object>() {100, "Dilantin", "Melanie", date}
            };

            var result = Csv.Create(new CreateInput() { InputType = CreateInputType.List, Delimiter = ";", Data = data, Headers = headers}, new CreateOption() { CultureInfo = "fi-FI" });
            Assert.AreEqual(result.Csv,
@"Dosage;Drug;Patient;Date
25;Indocin;David;1.1.2000 0.00.00
50;Enebrel;Sam;1.1.2000 0.00.00
10;Hydralazine;Christoff;1.1.2000 0.00.00
21;""Combiv;ent"";Janet;1.1.2000 0.00.00
100;Dilantin;Melanie;1.1.2000 0.00.00
");
        }

        [Test]
        public void TestWriteFromJson()
        {
            var json = @"[{""cool"":""nice"", ""what"": ""no""}, {""cool"":""not"", ""what"": ""yes""}, {""cool"":""maybe"", ""what"": ""never""}]";
            var result = Csv.Create(new CreateInput() { InputType = CreateInputType.Json, Delimiter = ";", Json = json}, new CreateOption());
            Assert.AreEqual(result.Csv,
@"cool;what
nice;no
not;yes
maybe;never
");
        }

        [Test]
        public void TestNullInputValue()
        {
            var json = @"[{""ShouldStayNull"":""null"", ""ShouldBeReplaced"": null}]";
            var result = Csv.Create(new CreateInput() { InputType = CreateInputType.Json, Delimiter = ";", Json = json }, new CreateOption() { ReplaceNullsWith = "replacedvalue" });
            Assert.AreEqual(result.Csv,
@"ShouldStayNull;ShouldBeReplaced
null;replacedvalue
");
        }

        [Test]
        public void TestNoQuotesOption()
        {
            var json = @"[{
""foo"" : "" Normally I would have quotes "",
""bar"" : ""I would not""
}]";
            var result2 = Csv.Create(new CreateInput() { InputType = CreateInputType.Json, Delimiter = ";", Json = json }, new CreateOption() { NeverAddQuotesAroundValues = false });
            Assert.AreEqual(result2.Csv,
                @"foo;bar
"" Normally I would have quotes "";I would not
");

            var result1 = Csv.Create(new CreateInput() { InputType = CreateInputType.Json, Delimiter = ";", Json = json }, new CreateOption() { NeverAddQuotesAroundValues = true });
            Assert.AreEqual(result1.Csv,
                @"foo;bar
 Normally I would have quotes ;I would not
");


      




        }

        [Test]
        public void TestDatetimeValue()
        {
            var json = @"[{
""datetime"" : ""2018-11-22T10:30:55"",
""string"" : ""foo""
}]";
            var result = Csv.Create(new CreateInput() { InputType = CreateInputType.Json, Delimiter = ";", Json = json }, new CreateOption() { });
            Assert.AreEqual(result.Csv,
@"datetime;string
2018-11-22T10:30:55;foo
");
        }

        [Test]
        public void TestDecimalValues()
        {
            var json = @"[{
""foo"" : 0.1,
""bar"" : 1.00,
""baz"" : 0.000000000000000000000000000000000000000000000000000000001
}]";
            var result = Csv.Create(new CreateInput() { InputType = CreateInputType.Json, Delimiter = ";", Json = json }, new CreateOption() { });
            Assert.AreEqual(result.Csv,
 @"foo;bar;baz
0.1;1.00;0.000000000000000000000000000000000000000000000000000000001
");
        }

        [Test]
        public void ParseAndWriteShouldUseSeparateCultures()
        {
            var csv =
@"First; Second; Number; Date
Foo; bar; 100; 2000-01-01";

            var parseResult = Csv.Parse(new ParseInput()
            {
                ColumnSpecifications = new[]
               {
                    new ColumnSpecification() {Name = "First", Type = ColumnType.String},
                    new ColumnSpecification() {Name = "Second", Type = ColumnType.String},
                    new ColumnSpecification() {Name = "Number", Type = ColumnType.Int},
                    new ColumnSpecification() {Name = "Date", Type = ColumnType.DateTime}
                },
                Delimiter = ";",
                Csv = csv
            }, new ParseOption() { ContainsHeaderRow = true, CultureInfo = "en-us" });

            var result = Csv.Create(new CreateInput() { InputType = CreateInputType.List, Delimiter = ";", Data = parseResult.Data, Headers = parseResult.Headers }, new CreateOption() { CultureInfo = "fi-FI" });

            Assert.AreEqual(result.Csv, 
                @"First;Second;Number;Date
Foo;"" bar"";100;1.1.2000 0.00.00
");
        }

        [Test]
        public void TestParseRowsWithAutomaticHeadersWhiteSpaceRemovalDefault()
        {
            var csv = $@"
year of the z;car;mark;price
1997;Ford;E350;2,34
2000;Mercury;Cougar;2,38";
            var result = Csv.Parse(new ParseInput()
            {
                ColumnSpecifications = new ColumnSpecification[0],
                Delimiter = ";",
                Csv = csv
            }, new ParseOption() { ContainsHeaderRow = true, SkipRowsFromTop = 0 });

            dynamic resultJArray = result.ToJson();
            var resultXml = result.ToXml();
            var resultData = result.Data;
            Assert.AreEqual(resultData.Count, 2);
            Assert.AreEqual(resultJArray.Count, 2);
            Assert.IsTrue(resultXml.Contains("<year of the z>"));
            Assert.AreEqual(resultJArray[0].price.ToString(), "2,34");
        }

        [Test]
        public void TestParseRowsWithAutomaticHeadersWhiteSpaceRemovalGiven()
        {
            var csv = $@"
year of the z;car;mark;price
1997;Ford;E350;2,34
2000;Mercury;Cougar;2,38";
            var result = Csv.Parse(new ParseInput()
            {
                ColumnSpecifications = new ColumnSpecification[0],
                Delimiter = ";",
                Csv = csv
            }, new ParseOption() { ContainsHeaderRow = true, SkipRowsFromTop = 0, ReplaceHeaderWhitespaceWith = "_" });

            dynamic resultJArray = result.ToJson();
            var resultXml = result.ToXml();
            var resultData = result.Data;
            Assert.AreEqual(resultData.Count, 2);
            Assert.AreEqual(resultJArray.Count, 2);
            Assert.IsTrue(resultXml.Contains("<year_of_the_z>"));
            Assert.AreEqual(resultJArray[0].price.ToString(), "2,34");
        }

        [Test]
        public void TestParseIgnoresQuotesInRowData()
        {
            var csv = "asdasd\nCoolio\nyear;car\";mark;price\n1997;Ford;E350;2,34\n2000;Mercury;Cougar;2,38";
            var result = Csv.Parse(new ParseInput()
            {
                ColumnSpecifications = new ColumnSpecification[0],
                Delimiter = ";",
                Csv = csv
            }, new ParseOption() { ContainsHeaderRow = true, SkipRowsFromTop = 2, SkipEmptyRows = false, IgnoreQuotes = true });

            dynamic resultJArray = result.ToJson();
            var resultXml = result.ToXml();
            var resultData = result.Data;
            Assert.AreEqual(resultData.Count, 2);
            Assert.AreEqual(resultJArray.Count, 2);
            Assert.IsTrue(resultXml.Contains("<year>2000</year>"));
            Assert.AreEqual(resultJArray[0].price.ToString(), "2,34");
        }

    }
}