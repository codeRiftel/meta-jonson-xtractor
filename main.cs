using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using jonson;

class Init {
    private static int Main(string[] args) {
        string line;
        StringBuilder inputBuilder = new StringBuilder();
        while ((line = Console.ReadLine()) != null) {
            inputBuilder.Append(line);
            inputBuilder.Append('\n');
        }

        string input = inputBuilder.ToString();

        var res = X.Tract(input);
        if (res.IsErr()) {
            var err = res.AsErr();
            Console.WriteLine("ERROR: " + err);
        } else {
            Console.WriteLine(Jonson.GeneratePretty(res.AsOk()));
        }

        return 0;
    }
}
