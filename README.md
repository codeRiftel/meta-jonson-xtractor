# meta-jonson-xtractor
X

## How to xtract?
0. mark your classes/structs by comment above declaration  
```csharp
using System.Collections.Generic;

// jonson.xtract <-- put this above class/struct
public struct Person {
    public string name;
    public int age;
    public bool dumb;
    public Job job;
    public string[] repos;
    public List<int> signature;
    public Dictionary<string, int> foo;
    public Number number;
    public Bar? bar;
    public int? nint;
}

public enum Number {
    Zero,
    One,
    Two,
    Three,
}

// jonson.xtract
public class Job {
    public string name;
    public string position;
    public decimal salary;
}

// jonson.xtract
public struct Bar {
    public string foo;
    public int foobar;
}
```
1. build (you need [option](https://github.com/codeRiftel/option) and [jonson](https://github.com/codeRiftel/jonson), just put option/ dir from the option package into root of this project as well as Jonson.cs from jonson package)  
`make`
2. run  
`cat *cs | mono xtractor.exe > description.json`
3. now you can use [meta-jonson](https://github.com/codeRiftel/meta-jonson) with description.json to generate C# code for parsing/generating JSON
