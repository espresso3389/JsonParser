JsonParser
==========

Small JSON parser implementation for .NET 3.0 or later

# What's this?

This is yet another JSON parser engine, on which I takes much care on the
footprint of implementation.
It does not introduces any JSON related classes but only JsonParser class,
which provides JSON parser/helper functions.

# How to use it

The following fragment explains how to use it:

```cs:parse.cs
// Parse JSON from file
var obj1 = JsonParser.ParseFile("sample.json", Encoding.UTF8);

// Parse JSON from string
var obj2 = JsonParser.Parse(
  "{ \"test\": [1,2,3,{ \"1\": { \"2\": [1, 2, 3, 4, 56, 57,55,445] } },{\"aa\": 12, \"ee\": \"aaa\"}, null, true, false] }");
```

Basically, JSON values are translated to the following .NET objects:

|JSON value type|.NET object type          |
|---------------|--------------------------|
|Objects        |Dictionary<string, object>|
|Arrays         |object[]                  |
|Numbers        |int/double                |
|Strings        |string                    |
|true/false     |bool                      |
|null           |object                    |

No special classes are introduced for representing JSON structure.


For the `obj2' case, the JSON is:

```json:sample.json
{
  "test": [1, 2, 3, {
      "1": {
        "2": [1, 2, 3, 4, 56, 57, 55, 445]
      }
    }, {
      "aa": 12,
      "ee": "aaa"
    },
    null, true, false
  ]
}
```

and then, you can get the values by specifying its path:

```cs:extract.cs
var v = JsonParser.JsonWalk(obj2, "test/3/1/2/2", -1);   // 3 (int)
var x = JsonParser.JsonWalk(obj2, "test/ccc/1/2/2", -1); // -1 (int)

// object[8]
var test = JsonParser.JsonWalk<object[]>(obj2, "test", null);

// of course, obj2 is Dictionary<string, object> in the case
var dict = (Dictionary<string, object>)obj2;
var test2 = dict["test"];
```
=======
Small JSON parser implementation for .NET 3.0 or later

# What's this?

This is yet another JSON parser engine, on which I takes much care on the
footprint of implementation.
It does not introduces any JSON related classes but only JsonParser class,
which provides JSON parser/helper functions.

# How to use it

The following fragment explains how to use it:

```cs:parse.cs
// Parse JSON from file
var obj1 = JsonParser.ParseFile("sample.json", Encoding.UTF8);

// Parse JSON from string
var obj2 = JsonParser.Parse(
  "{ \"test\": [1,2,3,{ \"1\": { \"2\": [1, 2, 3, 4, 56, 57,55,445] } },{\"aa\": 12, \"ee\": \"aaa\"}, null, true, false] }");
```

Basically, JSON values are translated to the following .NET objects:

|JSON value type  |.NET object type            |
|-----------------|----------------------------|
|Objects          |`Dictionary<string, object>`|
|Arrays           |`object[]`                  |
|Numbers          |`int/double`                |
|Strings          |`string`                    |
|`true/false`     |`bool`                      |
|`null`           |`object`                    |

No special classes are introduced for representing JSON structure.

For the `obj2' case, the JSON is:

```json:sample.json
{
  "test": [1, 2, 3, {
      "1": {
        "2": [1, 2, 3, 4, 56, 57, 55, 445]
      }
    }, {
      "aa": 12,
      "ee": "aaa"
    },
    null, true, false
  ]
}
```

and then, you can get the values by specifying its path:

```cs:extract.cs
var v = JsonParser.JsonWalk(obj2, "test/3/1/2/2", -1);   // 3 (int)
var x = JsonParser.JsonWalk(obj2, "test/ccc/1/2/2", -1); // -1 (int)

// object[8]
var test = JsonParser.JsonWalk<object[]>(obj2, "test", null);

// of course, obj2 is Dictionary<string, object> in the case
var dict = (Dictionary<string, object>)obj2;
var test2 = dict["test"];
```
