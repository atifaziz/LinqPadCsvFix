# LINQPad CSV Fix

LinqPadCsvFix is a console application designed to fix a single and specific
problem with [`lprun`][lprun], which is the command-line interface for running
[LINQPad][lp] queries.

## Problem and Solution

`lprun` supports multiple output formats, one of which is CSV. When a LINQPad
query is expressed as an [`IObservable`][IObservable] then `lprun` no longer
respects the CSV format and instead emits _individual_ JSON objects (or values
when `T` in `IObservable<T>` is a scalar). Suppose the following query:


```c#
from x in Observable.Range(1, 10)
select new { X = x, SquareOfX = x * x }
```

Running it with `lprun`, and in spite of specifying an output format of CSV
with the option `-format=csv`, the output streams out as individual JSON
objects:

    {
      "X": 1,
      "SquareOfX": 1
    }{
      "X": 2,
      "SquareOfX": 4
    }{
      "X": 3,
      "SquareOfX": 9
    }{
      "X": 4,
      "SquareOfX": 16
    }{
      "X": 5,
      "SquareOfX": 25
    }{
      "X": 6,
      "SquareOfX": 36
    }{
      "X": 7,
      "SquareOfX": 49
    }{
      "X": 8,
      "SquareOfX": 64
    }{
      "X": 9,
      "SquareOfX": 81
    }{
      "X": 10,
      "SquareOfX": 100
    }

Version 5.22 changed this slightly by having opening & closing braces
appearing on separate lines:

    {
      "X": 1,
      "SquareOfX": 1
    }
    {
      "X": 2,
      "SquareOfX": 4
    }
    {
      "X": 3,
      "SquareOfX": 9
    }
    {
      "X": 4,
      "SquareOfX": 16
    }
    {
      "X": 5,
      "SquareOfX": 25
    }
    {
      "X": 6,
      "SquareOfX": 36
    }
    {
      "X": 7,
      "SquareOfX": 49
    }
    {
      "X": 8,
      "SquareOfX": 64
    }
    {
      "X": 9,
      "SquareOfX": 81
    }
    {
      "X": 10,
      "SquareOfX": 100
    }

Either way, note that the output as a whole is not valid JSON so it cannot be
parsed such. Instead, each object has to be parsed out individually as JSON
and LinqPadCsvFix does exactly that followed by conversion to CSV. The output
above is fixed to:

    "X","SQUARE_OF_X"
    "1","1"
    "2","4"
    "3","9"
    "4","16"
    "5","25"
    "6","36"
    "7","49"
    "8","64"
    "9","81"
    "10","100"

LinqPadCsvFix expects the JSON members to use the Pascal naming convention,
which is the convention followed but properties of .NET types, but the CSV
output will use the `SCREAMING_SNAKE_CASE`. In the example above, `SquareOfX`
becomes `SQUARE_OF_X`.

Another problem with `lprun` is that if an error occurs while processing a
reactive query (i.e. based on `IObservable<T>`), it simply emits a JSON
representation of an `Exception` object. Moreover `lprun` ends with an exit
code of zero so it can give the false positive that the query finished
successfully. Suppose the following query:

```c#
from x in Observable.Range(-5, 10)
select new { X = x, Y = 100 / x }
```

When run via `lprun`, it will produce the following output:

    {
      "X": -5,
      "Y": -20
    }{
      "X": -4,
      "Y": -25
    }{
      "X": -3,
      "Y": -33
    }{
      "X": -2,
      "Y": -50
    }{
      "X": -1,
      "Y": -100
    }{
      "Message": "Attempted to divide by zero.",
      "Data": [],
      "InnerException": null,
      "TargetSite": {
        "Name": "<RunUserAuthoredQuery>b__0_0",
        "DeclaringType": "typeof(<>c)",
        "ReflectedType": "typeof(<>c)",
        "CustomAttributes": [],
        "MetadataToken": 100663307,
        "MethodImplementationFlags": 0,
        "MethodHandle": {
          "Value": {}
        },
        "Attributes": 131,
        "CallingConvention": 33,
        "IsGenericMethodDefinition": false,
        "ContainsGenericParameters": false,
        "IsGenericMethod": false,
        "IsSecurityCritical": true,
        "IsSecuritySafeCritical": false,
        "IsSecurityTransparent": false,
        "IsPublic": false,
        "IsPrivate": false,
        "IsFamily": false,
        "IsAssembly": true,
        "IsFamilyAndAssembly": false,
        "IsFamilyOrAssembly": false,
        "IsStatic": false,
        "IsFinal": false,
        "IsVirtual": false,
        "IsHideBySig": true,
        "IsAbstract": false,
        "IsSpecialName": false,
        "IsConstructor": false,
        "MemberType": 8,
        "ReturnType": "typeof(o)",
        "ReturnParameter": {
          "ParameterType": "typeof(o)",
          "Name": null,
          "HasDefaultValue": true,
          "DefaultValue": null,
          "RawDefaultValue": null,
          "Position": -1,
          "Attributes": 0,
          "Member": {},
          "IsIn": false,
          "IsOut": false,
          "IsLcid": false,
          "IsRetval": false,
          "IsOptional": false,
          "MetadataToken": 134217728,
          "CustomAttributes": []
        },
        "ReturnTypeCustomAttributes": {
          "ParameterType": "typeof(o)",
          "Name": null,
          "HasDefaultValue": true,
          "DefaultValue": null,
          "RawDefaultValue": null,
          "Position": -1,
          "Attributes": 0,
          "Member": {},
          "IsIn": false,
          "IsOut": false,
          "IsLcid": false,
          "IsRetval": false,
          "IsOptional": false,
          "MetadataToken": 134217728,
          "CustomAttributes": []
        }
      },
      "StackTrace": "   at UserQuery.<>c.<RunUserAuthoredQuery>b__0_0(Int32 x)\r\n   at System.Reactive.Linq.ObservableImpl.Select`2._.OnNext(TSource value)",
      "HelpLink": null,
      "Source": "LINQPadQuery",
      "HResult": -2147352558
    }

Note the exception object due to integer division by zero. LinqPadCsvFix
addresses this issue by looking for a JSON object that looks like an exception
and then ends immediately with a non-zero exit code upon encountering one. It
also emits the error message and stack trace from the exception to standard
error. So the fixed output from LinqPadCsvFix

    "X","Y"
    "-5","-20"
    "-4","-25"
    "-3","-33"
    "-2","-50"
    "-1","-100"
    Attempted to divide by zero.
       at UserQuery.<>c.<RunUserAuthoredQuery>b__0_0(Int32 x)
       at System.Reactive.Linq.ObservableImpl.Select`2._.OnNext(TSource value)


## Usage

    lpcsvfix [--debug] [COLUMN=PROPERTY]...

One or more `COLUMN=PROPERTY` arguments can be given to specify the output
CSV column name (`COLUMN`) for an object property (`PROPERTY`).

The `--debug` flag can be used to launch `lpcsvfix` attached to a debugger.

`lpcsvfix` expects the JSON stream to be fixed on standard input and emits the
fixed CSV to standard output and any warnings or errors to standard error. It
is therefore designed to be used in a pipe after `lprun` as shown below:

    lprun test.linq | lpcsvfix > test.csv

`lpcsvfix` will end with a non-zero exit code if it receives zero lines of
input.


## Limitations

- It is designed to work with JSON objects as output by `lprun` and
  illustrated earlier
- It does not work if the query output is primitive or scalar values
- The results are undefined if there is more than one reactive query being
  dumped, simultaneously or not.


[lp]: http://www.linqpad.net/
[lprun]: https://www.linqpad.net/lprun.aspx
[IObservable]: https://msdn.microsoft.com/en-us/library/dd990377.aspx