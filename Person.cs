using System.Collections.Generic;

// vjp.xtract
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

// vjp.xtract
public class Job {
    public string name;
    public string position;
    public decimal salary;
}

// vjp.xtract
public struct Bar {
    public string foo;
    public int foobar;
}
