using Microsoft.Data.Analysis;
using System.Collections;
using System.Diagnostics;
using System.Reflection;

// Darf nicht in einem Namespace liegen,
// weil Polyglot Notebooks es sonst nicht einbinden kann
// ReSharper disable once CheckNamespace 
public static class DataFrameHelper
{
    public static DataFrame ToDataFrame<T>(IEnumerable<T> list)
    {
        var dataFrame = new DataFrame();
        foreach (var property in typeof(T).GetProperties())
        {
            var columns = GetColumnType(property, list);
            foreach (var dataFrameColumn in columns)
            {
                dataFrame.Columns.Add(dataFrameColumn);
            }
        }

        return dataFrame;
    }

    private static IEnumerable<DataFrameColumn> GetColumnType<T>(PropertyInfo propertyInfo, IEnumerable<T> list,
        string namePrefix = "")
    {
        var dataType = GetBaseDataType(propertyInfo);
        var name = propertyInfo.Name;
        name = namePrefix + name;
        Debug.WriteLine($"Property: {name}, Type: {dataType}");

        if (Type.GetTypeCode(dataType) == TypeCode.Object && dataType != typeof(string))
        {
            foreach (var subProperty in dataType.GetProperties())
            {
                //if subproperty implements IEnumerable , skip it
                if (typeof(IEnumerable).IsAssignableFrom(subProperty.PropertyType))
                {
                    continue;
                }

                var sublist = Values2<T, object>(propertyInfo, list);
                foreach (var subColumn in GetColumnType<object>(subProperty, sublist, name + "_"))
                {
                    yield return subColumn;
                }
            }

            yield break;
        }

        if (dataType.IsEnum)
        {
            yield return new StringDataFrameColumn(name, EnumToString<T>(propertyInfo, list));
            yield break;
        }


        yield return Type.GetTypeCode(dataType) switch
        {
            TypeCode.Boolean => new BooleanDataFrameColumn(name, Values<T, bool>(propertyInfo, list)),
            TypeCode.Byte => new ByteDataFrameColumn(name, Values<T, byte>(propertyInfo, list)),
            TypeCode.SByte => new SByteDataFrameColumn(name, Values<T, sbyte>(propertyInfo, list)),
            TypeCode.Int16 => new Int16DataFrameColumn(name, Values<T, short>(propertyInfo, list)),
            TypeCode.UInt16 => new UInt16DataFrameColumn(name, Values<T, ushort>(propertyInfo, list)),
            TypeCode.Int32 => new Int32DataFrameColumn(name, Values<T, int>(propertyInfo, list)),
            TypeCode.UInt32 => new UInt32DataFrameColumn(name, Values<T, uint>(propertyInfo, list)),
            TypeCode.Int64 => new Int64DataFrameColumn(name, Values<T, long>(propertyInfo, list)),
            TypeCode.UInt64 => new UInt64DataFrameColumn(name, Values<T, ulong>(propertyInfo, list)),
            TypeCode.Single => new SingleDataFrameColumn(name, Values<T, float>(propertyInfo, list)),
            TypeCode.Double => new DoubleDataFrameColumn(name, Values<T, double>(propertyInfo, list)),
            TypeCode.Decimal => new DecimalDataFrameColumn(name, Values<T, decimal>(propertyInfo, list)),
            TypeCode.DateTime => new DateTimeDataFrameColumn(name, Values<T, DateTime>(propertyInfo, list)),
            TypeCode.String => new StringDataFrameColumn(name, Values2<T, string>(propertyInfo, list)),
            TypeCode.Char => new CharDataFrameColumn(name, Values<T, char>(propertyInfo, list)),
            TypeCode.Empty => throw new NotSupportedException(),
            TypeCode.DBNull => throw new NotSupportedException(),
            TypeCode.Object =>
                throw new NotSupportedException(), //wird oben schon abgefangen, hier nur zur Vollständigkeit
            _ => throw new ArgumentException($"Unsupported type: {dataType}")
        };
   }

    //struct, damit das Default(TOut?) funktioniert und null zurückliefert
    private static IEnumerable<TOut?> Values<T, TOut>(PropertyInfo propertyInfo, IEnumerable<T> list)
        where TOut : struct
    {
        foreach (var item in list)
        {
            if (item == null || propertyInfo.GetValue(item) == null)
            {
                var x = default(TOut?);
                yield return x;
            }
            else
            {
                var val = propertyInfo.GetValue(item)!;
                //check if val is an enum, if yes, convert it to int
                if (val.GetType().IsEnum)
                {
                    val = (int)val;
                }

                Console.WriteLine(val.GetType());
                yield return (TOut?)val;
            }
        }
    }

    private static IEnumerable<TOut?> Values2<T, TOut>(PropertyInfo propertyInfo, IEnumerable<T> list)
        where TOut : class
    {
        foreach (var item in list)
        {
            if (propertyInfo.GetValue(item) == null)
            {
                var x = default(TOut?);
                yield return x;
            }
            else
            {
                yield return (TOut?)propertyInfo.GetValue(item)!;
            }
        }
    }
    
    private static IEnumerable<string?> EnumToString<T>(PropertyInfo propertyInfo, IEnumerable<T> list)
    {
        foreach (var item in list)
        {
            if (propertyInfo.GetValue(item) == null)
            {
                yield return null;
            }
            else
            {
                yield return propertyInfo.GetValue(item)!.ToString();
            }
        }
    }

    private static Type GetBaseDataType(PropertyInfo property) =>
        property.PropertyType.IsGenericType &&
        property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>)
            ? Nullable.GetUnderlyingType(property.PropertyType)!
            : property.PropertyType;
}

