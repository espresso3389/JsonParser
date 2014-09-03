/*
Small JSON parser implementation for .NET 3.0 or later

Copyright (c) 2014, Takashi Kawasaki
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, this
  list of conditions and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice,
  this list of conditions and the following disclaimer in the documentation
  and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace SimpleJsonParser
{
    public class JsonParser
    {
        public static object Parse(TextReader jsonReader)
        {
            var p = new JsonParser(jsonReader);
            return p.Parse();
        }

        public static object Parse(string json)
        {
            return Parse(new StringReader(json));
        }

        public static object Parse(Stream json)
        {
            return Parse(new StreamReader(json));
        }

        public static object ParseFile(string jsonFile, Encoding encoding)
        {
            using (var sr = new StreamReader(File.OpenRead(jsonFile), encoding))
            return Parse(sr);
        }

        /// <summary>
        /// Get the object on the specified path.
        /// Please note that array index starts with 0.
        /// </summary>
        /// <typeparam name="T">Type of the object.</typeparam>
        /// <param name="obj">Parsed JSON object.</param>
        /// <param name="path">Object path in the JSON object.</param>
        /// <param name="defValue">The default value for not found case.</param>
        /// <returns>The object. If the object is found but the type is different, the <paramref name="defValue"/> is returned.</returns>
        public static T JsonWalk<T>(object obj, string path, T defValue)
        {
            return JsonWalk<T>(obj, defValue, path.Split('/'), 0);
        }

        public static T JsonWalk<T>(object obj, T defValue, string[] path, int index)
        {
            if (index > path.Length)
            {
                return defValue;
            }
            else if (index == path.Length)
            {
                if (obj is T)
                    return (T)obj;
                return defValue;
            }
            else
            {
                int arrIndex;
                if (int.TryParse(path[index], out arrIndex))
                {
                    var arr = obj as object[];
                    if (arr != null)
                        return JsonWalk<T>(arr[arrIndex], defValue, path, ++index);
                }

                var dict = obj as Dictionary<string, object>;
                if (dict == null)
                    return defValue;
                object v;
                if (!dict.TryGetValue(path[index], out v))
                    return defValue;
                return JsonWalk<T>(v, defValue, path, ++index);
            }
        }

        private JsonParser(TextReader sr)
        {
            reader = sr;
        }

        TextReader reader;

        const int NoChar = -1;

        string fileName = "unknown.json";
        int lastReadChar = NoChar;
        int backingChar = NoChar;
        int line = 1;
        int column = 0;

        int readChar()
        {
            column++;
            if (backingChar != NoChar)
            {
                var c = lastReadChar = backingChar;
                backingChar = NoChar;
                return c;
            }
            else
            {
                var c = reader.Read();
                lastReadChar = c;
                return c;
            }
        }

        void unreadChar(int c)
        {
            if (backingChar != NoChar)
                throw new InvalidOperationException("Duplicate unread of character.");
            backingChar = c;
            column--;
        }

        void unreadChar()
        {
            unreadChar(lastReadChar);
            lastReadChar = NoChar;
        }

        object Parse()
        {
            var contexts = new Stack<object>();
            var sb = new StringBuilder();
            object obj = null;
            var keys = new Stack<string>();
            while (true)
            {
                var c = readChar();
                if (c == '"')
                {
                    if (obj != null || sb.Length > 0)
                        throw dataException("Unexpected '\"'.");
                    obj = readString();
                }
                else if (c == '[')
                {
                    if (obj != null || sb.Length > 0)
                        throw dataException("Unexpected '['.");
                    contexts.Push(new List<object>());
                }
                else if (c == '{')
                {
                    if (obj != null || sb.Length > 0)
                        throw dataException("Unexpected '{'.");
                    contexts.Push(new Dictionary<string, object>());
                }
                else if (c == ',')
                {
                    if (obj == null && sb.Length == 0)
                        throw dataException("Unexpected ','.");

                    obj = obj ?? parseString(sb.ToString());

                    var container = contexts.Count > 0 ? contexts.Peek() : null;
                    var list = container as List<object>;
                    if (list != null)
                    {
                        list.Add(transform(obj));
                        obj = null;
                        sb = new StringBuilder();
                        continue;
                    }
                    var dict = container as Dictionary<string, object>;
                    if (dict != null)
                    {
                        if (keys.Count == 0)
                                throw dataException("Unexpected ','.");
                        dict.Add(keys.Pop(), transform(obj));
                        obj = null;
                        sb = new StringBuilder();
                        continue;
                    }

                    throw dataException("Unexpected ','.");
                }
                else if (c == ':')
                {
                    var key = obj as string;
                    if (key == null || sb.Length > 0)
                        throw dataException("Unexpected ':'.");
                    obj = null;
                    keys.Push(key);
                }
                else if (c == ']')
                {
                    var arr = contexts.Count > 0 ? contexts.Pop() as List<object> : null;
                    if (arr == null)
                        throw dataException("Unexpected ']'.");
                    if (obj != null || sb.Length > 0)
                        arr.Add(transform(obj ?? parseString(sb.ToString())));

                    if (contexts.Count == 0)
                        return arr.ToArray();
                    
                    obj = arr.ToArray();
                    sb = new StringBuilder();
                }
                else if (c == '}')
                {
                    var dict = contexts.Count > 0 ? contexts.Pop() as Dictionary<string, object> : null;
                    if (dict == null)
                        throw dataException("Unmatched '}'.");
                    if (obj != null || sb.Length > 0)
                    {
                        if (keys.Count == 0)
                            throw dataException("Unexpected '}'.");
                        dict.Add(keys.Pop(), transform(obj ?? parseString(sb.ToString())));
                        obj = null;
                        sb = new StringBuilder();
                    }
                    if (contexts.Count == 0)
                        return dict;
                    else
                        obj = dict;
                }
                else if (c == NoChar || " \t\r\n".IndexOf((char)c) >= 0)
                {
                    if (sb.Length > 0)
                    {
                        if (obj != null)
                            throw dataException(string.Format("Unexpected literal '{0}'.", sb.ToString()));

                        obj = parseString(sb.ToString());
                        sb = new StringBuilder();
                    }

                    if (c == NoChar)
                    {
                        return obj;
                    }
                    else if (c == '\r')
                    {
                        if (readChar() != '\n')
                            unreadChar();
                        line++;
                        column = 0;
                    }
                    else if (c == '\n')
                    {
                        line++;
                        column = 0;
                    }
                }
                else
                {
                    sb.Append((char)c);
                }
            }
        }

        class Null
        {
            public static readonly Null _null = new Null();
            private Null() { }
        };

        object transform(object obj)
        {
            if (obj is Null)
                return null;
            return obj;
        }

        object parseString(string s)
        {
            if (s == "true")
                return true;
            if (s == "false")
                return false;
            if (s == "null")
                return Null._null;
            int v;
            if (int.TryParse(s, out v))
                return v;
            return double.Parse(s);
        }

        string readString()
        {
            var sb = new StringBuilder();
            for (; ; )
            {
                var c = readChar();
                switch (c)
                {
                    case NoChar:
                        throw dataException("string sequence does not terminated correctly.");
                    case '"':
                        return sb.ToString();
                    case '\\':
                        switch (c = readChar())
                        {
                            case '"': sb.Append('"'); break;
                            case '\\': sb.Append('\\'); break;
                            case '/': sb.Append('/'); break;
                            case 'b': sb.Append('\b'); break;
                            case 'f': sb.Append('\f'); break;
                            case 'n': sb.Append('\n'); break;
                            case 'r': sb.Append('\r'); break;
                            case 't': sb.Append('\t'); break;
                            case 'u':
                                {
                                    var sbHex4 = new StringBuilder(4);
                                    sbHex4.Append((char)readChar());
                                    sbHex4.Append((char)readChar());
                                    sbHex4.Append((char)readChar());
                                    sbHex4.Append((char)readChar());
                                    sb.Append((char)int.Parse(sbHex4.ToString(), System.Globalization.NumberStyles.AllowHexSpecifier));
                                    break;
                                }
                            default:
                                throw dataException(string.Format("Invalid escape sequence \\{0}", (char)c));
                        }
                        break;
                    default:
                        sb.Append((char)c);
                        break;
                }
            }
        }

        Exception dataException(string errorMessage)
        {
            return new Exception(string.Format("{0}({1},{2}): {3}", fileName, line, column, errorMessage));
        }
    }
}
