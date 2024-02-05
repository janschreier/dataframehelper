# ToDataFrame()-Helper
A helper class to transform an IEnumerable&lt;T> to a Microsoft.Data.Analysis DataFrame

This class/method is more of a snipped rather than a fully fledged solution. 

# What it does

Takes any `List<T>` and outputs a `DataFrame` as [defined in ML.net](https://learn.microsoft.com/en-us/dotnet/api/microsoft.data.analysis.dataframe?view=ml-dotnet-preview) with the properties as column names

- Enums are returned with their ToString()-Value
- Object-Properties are flattend and prefixed with the object's name:
```csharp
//input format

public class Person {
  public Address Address {get; set;}
  public int PersonId {get;set;}
}

public class Address {
public string ZipCode {get; set; }
public string Street {get; set; }
// ... more
}


//output as DataFrame with the following column-names:
Address_ZipCode, Address_Street, PersonId
```
