using System.Collections.Generic;

// jonson.xtract
public struct Person {
    public string name;
    public bool dumb;
    public Job job;
    public string[] repos;
    public List<int> signature;
    public Dictionary<string, int> foo;
    public Dictionary<string, List<string>> whatever;
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
}
