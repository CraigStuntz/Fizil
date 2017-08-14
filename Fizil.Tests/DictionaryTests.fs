﻿namespace Fizil.Tests

open NUnit.Framework
open FsUnit

module DictionaryTests =

    let exampleDictionary = """#
# AFL dictionary for JSON
# -----------------------------
#

object_start="{"
object_end="}"
object_empty="{}"
object_one_element="{\"one\":1}"
object_two_elements="{\"1\":1,\"2\":2}"
object_separator=":"

array_start="["
array_end="]"
array_empty="[]"
array_one_element="[1]"
array_two_elements="[1,2]"

separator=","

escape_sequence_b="\\b"
escape_sequence_f="\\f"
escape_sequence_n="\\n"
escape_sequence_r="\\r"
escape_sequence_t="\\t"
escape_sequence_quote="\\\""
escape_sequence_backslash="\\\\"
escape_sequence_slash="\\/"
escape_sequence_utf16_base="\\u"
escape_sequence_utf16="\\u12ab"

number_integer="1"
number_double="1.0"
number_negative_integer="-1"
number_negative_double="-1.0"
number_engineering1="1e1"
number_engineering2="1e-1"
number_positive_integer="+1"
number_positive_double="+1.0"
number_e="e"
number_plus="+"
number_minus="-"
number_separator="."

null="null"
true="true"
false="false"
"""

    let toLines (str: string) = System.Text.RegularExpressions.Regex.Split(str, "\r\n|\r|\n");

    let expected = [|
        "{";
        "}";
        "{}";
        "{\"one\":1}";
        "{\"1\":1,\"2\":2}";
        ":";
        "[";
        "]";
        "[]";
        "[1]";
        "[1,2]";
        ",";
        "\\b";
        "\\f";
        "\\n";
        "\\r";
        "\\t";
        "\\\"";
        "\\\\";
        "\\/";
        "\\u";
        "\\u12ab";
        "1";
        "1.0";
        "-1";
        "-1.0";
        "1e1";
        "1e-1";
        "+1";
        "+1.0";
        "e";
        "+";
        "-";
        ".";
        "null";
        "true";
        "false"
    |]

    [<Test>]
    let ``parses JSON dictionary``() = 
        exampleDictionary 
            |> toLines
            |> Dictionary.readStrings
            |> should equal expected