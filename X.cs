using System;
using System.Collections.Generic;
using jonson;
using option;
using System.Text;

public static class X {
    public enum Tracterr {
        NullSource,
        EmptySource,
        UnknownToken
    }

    private enum Token {
        Unknown,
        Word,
        String,
        Semicolon,
        OpenCurly,
        CloseCurly,
        OpenAngle,
        CloseAngle,
        Comma,
        LineComment,
        BlockComment,
        Division,
        EOF
    }

    private struct LexRes {
        public Token token;
        public int start;
        public int len;
    }

    private enum ParseState {
        OutType,
        InType
    }

    private struct GenInfo {
        public string name;
        public List<string> types;
    }

    public static Result<JSONType, Tracterr> Tract(string source) {
        if (source == null) {
            return Result<JSONType, Tracterr>.Err(Tracterr.NullSource);
        }

        if (source.Length == 0) {
            return Result<JSONType, Tracterr>.Err(Tracterr.EmptySource);
        }

        var root = new Dictionary<string, JSONType>();
        Dictionary<string, JSONType> current = null;
        var state = ParseState.OutType;
        LexRes lexRes = new LexRes();
        int pos = 0;
        int opened = 0;

        var slaves = new HashSet<string>();

        var enumSet = LookUpEnums(source);
        if (enumSet.Count > 0) {
            slaves.Add("int");
        }

        while (lexRes.token != Token.EOF) {
            lexRes = Lex(source, pos);
            if (lexRes.token == Token.Unknown) {
                return Result<JSONType, Tracterr>.Err(Tracterr.UnknownToken);
            }

            pos = lexRes.start + lexRes.len;

            if (state == ParseState.OutType) {
                if (lexRes.token == Token.LineComment) {
                    var comment = source.Substring(lexRes.start, lexRes.len);
                    var dec = lexRes.start + lexRes.len;
                    lexRes = Lex(source, dec);
                    if (lexRes.token == Token.Word && comment.Contains("jonson.xtract")) {
                        var mod = source.Substring(lexRes.start, lexRes.len).Trim();
                        dec = lexRes.start + lexRes.len;
                        var typeRes = Lex(source, dec);

                        dec = typeRes.start + typeRes.len;
                        var nameRes = Lex(source, dec);

                        dec = nameRes.start + nameRes.len;
                        var openRes = Lex(source, dec);

                        bool isOk = true;
                        isOk = isOk && typeRes.token == Token.Word;
                        isOk = isOk && nameRes.token == Token.Word;
                        isOk = isOk && openRes.token == Token.OpenCurly;

                        if (isOk) {
                            var type = source.Substring(typeRes.start, typeRes.len).Trim();
                            if (mod == "public" && (type == "class" || type == "struct")) {
                                var name = source.Substring(nameRes.start, nameRes.len).Trim();
                                state = ParseState.InType;

                                current = new Dictionary<string, JSONType>();

                                if (type == "class") {
                                    current["__is_ref"] = JSONType.Make(true);
                                }

                                root[name] = JSONType.Make(current);

                                opened = 1;

                                pos = openRes.start + openRes.len;
                            }
                        }
                    }
                }
            } else {
                if (lexRes.token == Token.OpenCurly) {
                    opened++;
                }

                if (lexRes.token == Token.CloseCurly) {
                    opened--;
                }

                if (lexRes.token == Token.Word) {
                    var field = lexRes.start + lexRes.len;
                    var mod = source.Substring(lexRes.start, lexRes.len);

                    var typeRes = Lex(source, field);

                    string type = null;

                    if (mod == "public" && typeRes.token == Token.Word) {
                        field = typeRes.start + typeRes.len;
                        var nextTokenRes = Lex(source, field);

                        var full = new Stack<GenInfo>();

                        if (nextTokenRes.token == Token.OpenAngle) {
                            full.Push(
                                new GenInfo {
                                    name = source.Substring(typeRes.start, typeRes.len),
                                    types = new List<string>()
                                }
                            );

                            field = nextTokenRes.start + nextTokenRes.len;

                            while (true) {
                                nextTokenRes = Lex(source, field);

                                if (nextTokenRes.token == Token.EOF) {
                                    break;
                                } else if (nextTokenRes.token == Token.Word) {
                                    var startWord = nextTokenRes.start;
                                    var lenWord = nextTokenRes.len;
                                    var genWord = source.Substring(startWord, lenWord);
                                    full.Peek().types.Add(genWord);
                                } else if (nextTokenRes.token == Token.OpenAngle) {
                                    var types = full.Peek().types;
                                    var lastWord = types[types.Count - 1];
                                    full.Peek().types.RemoveAt(types.Count - 1);
                                    full.Push(
                                        new GenInfo {
                                            name = lastWord,
                                            types = new List<string>()
                                        }
                                    );
                                } else if (nextTokenRes.token == Token.CloseAngle) {
                                    var fullBuilder = new StringBuilder();
                                    var genInfo = full.Pop();
                                    fullBuilder.Append(genInfo.name);
                                    fullBuilder.Append('<');

                                    for (int i = 0; i < genInfo.types.Count; i++) {
                                        if (i > 0) {
                                            fullBuilder.Append(", ");
                                        }
                                        var subtype = genInfo.types[i];
                                        fullBuilder.Append(subtype);

                                        if (IsPrimitive(subtype) || IsCollection(subtype)) {
                                            slaves.Add(subtype);
                                        }
                                    }

                                    fullBuilder.Append('>');

                                    if (full.Count > 0) {
                                        full.Peek().types.Add(fullBuilder.ToString());
                                    } else {
                                        type = fullBuilder.ToString();
                                        break;
                                    }
                                }

                                field = nextTokenRes.start + nextTokenRes.len;
                            }

                            field = nextTokenRes.start + nextTokenRes.len;
                        } else if (nextTokenRes.token == Token.Word) {
                            type = source.Substring(typeRes.start, typeRes.len);
                            if (type.EndsWith("[]")) {
                                var subtype = type.Substring(0, type.Length - 2);
                                if (IsPrimitive(subtype)) {
                                    slaves.Add(subtype);
                                }
                            }
                        }

                        var nameRes = Lex(source, field);

                        field = nameRes.start + nameRes.len;
                        var semicolonRes = Lex(source, field);

                        var isOk = nameRes.token == Token.Word;
                        isOk = isOk && semicolonRes.token == Token.Semicolon;
                        if (isOk) {
                            pos = semicolonRes.start + semicolonRes.len;
                            var name = source.Substring(nameRes.start, nameRes.len);

                            if (enumSet.Contains(type)) {
                                type = "enum " + type;
                            }

                            current[name] = JSONType.Make(type);
                        }
                    }
                }

                if (opened == 0) {
                    state = ParseState.OutType;
                }
            }
        }

        if (slaves.Count > 0) {
            var slaveTypes = new List<JSONType>();
            foreach (var slave in slaves) {
                slaveTypes.Add(JSONType.Make(slave));
            }

            root["__slaves"] = JSONType.Make(slaveTypes);
        }

        return Result<JSONType, Tracterr>.Ok(JSONType.Make(root));
    }

    private static HashSet<string> LookUpEnums(string source) {
        var enumSet = new HashSet<string>();

        var pos = 0;
        while (true) {
            var lexRes = Lex(source, pos);

            if (lexRes.token == Token.EOF) {
                break;
            }

            pos = lexRes.start + lexRes.len;

            if (lexRes.token == Token.Word) {
                var word = source.Substring(lexRes.start, lexRes.len);
                if (word == "enum") {
                    var nameRes = Lex(source, pos);
                    if (nameRes.token == Token.Word) {
                        var name = source.Substring(nameRes.start, nameRes.len);
                        enumSet.Add(name);

                        pos = nameRes.start + nameRes.len;
                    }
                }
            }
        }

        return enumSet;
    }

    private static LexRes Lex(string source, int start) {
        var res = new LexRes();
        var pos = start;
        bool eof = false;
        char c;

        while (IsWhiteSpace(source[pos])) {
            var nextOpt = NextChar(source, ref pos);
            if (nextOpt.IsNone()) {
                eof = true;
                break;
            }
        }

        if (eof) {
            res.token = Token.EOF;
            return res;
        }

        switch (source[pos]) {
            case ';':
                res.start = pos;
                res.len = 1;
                res.token = Token.Semicolon;
                break;
            case '{':
                res.start = pos;
                res.len = 1;
                res.token = Token.OpenCurly;
                break;
            case '}':
                res.start = pos;
                res.len = 1;
                res.token = Token.CloseCurly;
                break;
            case '/':
                var startComment = pos;
                var nextComChar = NextChar(source, ref pos);
                if (nextComChar.IsNone()) {
                    res.token = Token.EOF;
                } else {
                    c = nextComChar.Peel();
                    if (c == '/') {
                        while (c != '\n') {
                            nextComChar = NextChar(source, ref pos);
                            if (nextComChar.IsNone()) {
                                res.token = Token.EOF;
                                break;
                            } else {
                                c = nextComChar.Peel();
                            }
                        }

                        if (res.token != Token.EOF) {
                            res.start = startComment;
                            res.len = pos - startComment;
                            res.token = Token.LineComment;
                        }
                    } else if (c == '*') {
                        while (true) {
                            nextComChar = NextChar(source, ref pos);
                            if (nextComChar.IsNone()) {
                                res.token = Token.EOF;
                                break;
                            } else {
                                c = nextComChar.Peel();

                                if (c == '*') {
                                    var tempPos = pos;
                                    nextComChar = NextChar(source, ref tempPos);
                                    if (nextComChar.IsNone()) {
                                        res.token = Token.EOF;
                                        break;
                                    } else {
                                        var maybeSlash = nextComChar.Peel();
                                        if (maybeSlash == '/') {
                                            pos = tempPos;
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        if (res.token != Token.EOF) {
                            res.start = startComment;
                            res.len = pos - startComment;
                            res.token = Token.BlockComment;
                        }
                    } else {
                        res.start = startComment;
                        res.len = 1;
                        res.token = Token.Division;
                    }
                }
                break;
            case '"':
                var startString = pos;
                var nextStrChar = NextChar(source, ref pos);
                if (nextStrChar.IsNone()) {
                    res.token = Token.EOF;
                } else {
                    c = nextStrChar.Peel();
                    while (c != '"') {
                        nextStrChar = NextChar(source, ref pos);
                        if (nextStrChar.IsNone()) {
                            eof = true;
                            break;
                        }

                        c = nextStrChar.Peel();
                    }

                    if (eof) {
                        res.token = Token.EOF;
                    } else {
                        res.start = startString;
                        res.len = pos - startString + 1;
                        res.token = Token.String;
                    }
                }
                break;
            case '<':
                res.start = pos;
                res.len = 1;
                res.token = Token.OpenAngle;
                break;
            case '>':
                res.start = pos;
                res.len = 1;
                res.token = Token.CloseAngle;
                break;
            case ',':
                res.start = pos;
                res.len = 1;
                res.token = Token.Comma;
                break;
            default:
                if (eof) {
                    res.token = Token.EOF;
                } else {
                    var startWord = pos;
                    c = source[pos];
                    while (!IsWhiteSpace(c) && !IsSpecial(c)) {
                        var nextOpt = NextChar(source, ref pos);
                        if (nextOpt.IsNone()) {
                            eof = true;
                            break;
                        }

                        c = nextOpt.Peel();
                    }

                    if (eof) {
                        res.token = Token.EOF;
                    } else {
                        res.start = startWord;
                        res.len = pos - startWord;
                        res.token = Token.Word;
                    }
                }
                break;
        }

        return res;
    }

    private static Option<char> NextChar(string source, ref int pos) {
        pos++;
        if (pos >= source.Length) {
            return Option<char>.None();
        }

        return Option<char>.Some(source[pos]);
    }

    private static bool IsSpecial(char c) {
        var isSpec = c == ';' || c == '"' || c == '{' || c == '}' || c == ',' || c == '<';
        isSpec = isSpec || c == '>' || c == '/';
        return isSpec;
    }

    private static bool IsWhiteSpace(char c) {
        return c == ' ' || c == '\t' || c == '\n';
    }

    private static bool IsCollection(string type) {
        return type.StartsWith("Dictionary<") || type.StartsWith("List<");
    }

    private static bool IsPrimitive(string type) {
        return type == "string" || type == "int" || type == "float" || type == "double" ||
            type == "decimal" || type == "sbyte" || type == "byte" || type == "short" ||
            type == "ushort" || type == "uint" || type == "long" || type == "ulong" ||
            type == "bool";
    }
}
