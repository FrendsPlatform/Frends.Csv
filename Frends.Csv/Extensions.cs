using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#pragma warning disable 1591

namespace Frends.Csv
{
    public static class Extensions
    {
        public static Type ToType(this ColumnType code)
        {
            switch (code)
            {
                case ColumnType.Boolean:
                    return typeof(bool);

                case ColumnType.Char:
                    return typeof(char);

                case ColumnType.DateTime:
                    return typeof(DateTime);

                case ColumnType.Decimal:
                    return typeof(decimal);

                case ColumnType.Double:
                    return typeof(double);

                case ColumnType.Int:
                    return typeof(int);

                case ColumnType.Long:
                    return typeof(long);

                case ColumnType.String:
                    return typeof(string);

            }

            return null;
        }
    }
}
