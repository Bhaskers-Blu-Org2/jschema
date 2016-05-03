﻿// Copyright (c) Microsoft Corporation.  All Rights Reserved.
// Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Json.Schema.ToDotNet.UnitTests;
using Xunit;
using Xunit.Abstractions;
using Assert = Microsoft.Json.Schema.ToDotNet.UnitTests.Assert;

namespace Microsoft.Json.Schema.ToDotNet.Hints.UnitTests
{
    public class ClassNameHintTests
    {
        private const string PrimaryOutputFilePath = TestFileSystem.OutputDirectory + "\\" + TestSettings.RootClassName + ".cs";

        private readonly TestFileSystem _testFileSystem;
        private readonly DataModelGeneratorSettings _settings;

        public ClassNameHintTests()
        {
            _testFileSystem = new TestFileSystem();
            _settings = TestSettings.MakeSettings();
        }

        public class TestCase
        {
            public TestCase(
                string name,
                string schemaText,
                string hintedClassName,
                string hintsText,
                string primaryClassText,
                string primaryClassComparerText,
                string hintedClassText,
                string hintedClassComparerText)
            {
                Name = name;
                SchemaText = schemaText;
                HintedClassName = hintedClassName;
                HintsText = hintsText;
                PrimaryClassText = primaryClassText;
                PrimaryClassComparerText = primaryClassComparerText;
                HintedClassText = hintedClassText;
                HintedClassComparerText = hintedClassComparerText;
            }

            public TestCase()
            {
                // Needed for deserialization.
            }

            public string Name;
            public string SchemaText;
            public string HintedClassName;
            public string HintsText;
            public string PrimaryClassText;
            public string PrimaryClassComparerText;
            public string HintedClassText;
            public string HintedClassComparerText;

            public void Deserialize(IXunitSerializationInfo info)
            {
                Name = info.GetValue<string>(nameof(Name));
                SchemaText = info.GetValue<string>(nameof(SchemaText));
                HintedClassName = info.GetValue<string>(nameof(HintedClassName));
                HintsText = info.GetValue<string>(nameof(HintsText));
                PrimaryClassText = info.GetValue<string>(nameof(PrimaryClassText));
                PrimaryClassComparerText = info.GetValue<string>(nameof(PrimaryClassComparerText));
                HintedClassText = info.GetValue<string>(nameof(HintedClassText));
                HintedClassComparerText = info.GetValue<string>(nameof(HintedClassComparerText));
            }

            public void Serialize(IXunitSerializationInfo info)
            {
                info.AddValue(nameof(Name), Name);
                info.AddValue(nameof(SchemaText), SchemaText);
                info.AddValue(nameof(HintedClassName), HintedClassName);
                info.AddValue(nameof(HintsText), HintsText);
                info.AddValue(nameof(PrimaryClassText), PrimaryClassText);
                info.AddValue(nameof(PrimaryClassComparerText), PrimaryClassComparerText);
                info.AddValue(nameof(HintedClassText), HintedClassText);
                info.AddValue(nameof(HintedClassComparerText), HintedClassComparerText);
            }

            public override string ToString()
            {
                return Name;
            }
        }

        public static readonly TheoryData<TestCase> TestCases = new TheoryData<TestCase>
        {
            new TestCase(
                "Change class name",
@"{
  ""type"": ""object"",
  ""properties"": {
    ""file"": {
      ""$ref"": ""#/definitions/file""
    }
  },
  ""definitions"": {
    ""file"": {
      ""type"": ""object"",
      ""properties"": {
        ""path"": {
          ""type"": ""string""
        }
      }
    }
  }
}",

    "FileData",

@"{
  ""file"": [
    {
      ""kind"": ""ClassNameHint"",
      ""arguments"": {
        ""className"": ""FileData""
      }
    }
  ]
}",

// PrimaryClassText
@"using System;
using System.CodeDom.Compiler;
using System.Runtime.Serialization;

namespace N
{
    [DataContract]
    [GeneratedCode(""Microsoft.Json.Schema.ToDotNet"", """ + VersionConstants.FileVersion + @""")]
    public partial class C
    {
        [DataMember(Name = ""file"", IsRequired = false, EmitDefaultValue = false)]
        public FileData File { get; set; }
    }
}",

// PrimaryClassComparerText
@"using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;

namespace N
{
    /// <summary>
    /// Defines methods to support the comparison of objects of type C for equality.
    /// </summary>
    [GeneratedCode(""Microsoft.Json.Schema.ToDotNet"", """ + VersionConstants.FileVersion + @""")]
    public sealed class CEqualityComparer : IEqualityComparer<C>
    {
        public static readonly CEqualityComparer Instance = new CEqualityComparer();

        public bool Equals(C left, C right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
            {
                return false;
            }

            var fileDataEqualityComparer = new FileDataEqualityComparer();
            if (!fileDataEqualityComparer.Equals(left.File, right.File))
            {
                return false;
            }

            return true;
        }

        public int GetHashCode(C obj)
        {
            if (ReferenceEquals(obj, null))
            {
                return 0;
            }

            int result = 17;
            unchecked
            {
                if (obj.File != null)
                {
                    result = (result * 31) + obj.File.GetHashCode();
                }
            }

            return result;
        }
    }
}",

// HintedClassText
@"using System;
using System.CodeDom.Compiler;
using System.Runtime.Serialization;

namespace N
{
    [DataContract]
    [GeneratedCode(""Microsoft.Json.Schema.ToDotNet"", """ + VersionConstants.FileVersion + @""")]
    public partial class FileData
    {
        [DataMember(Name = ""path"", IsRequired = false, EmitDefaultValue = false)]
        public string Path { get; set; }
    }
}",

// HintedClassComparerText
@"using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;

namespace N
{
    /// <summary>
    /// Defines methods to support the comparison of objects of type FileData for equality.
    /// </summary>
    [GeneratedCode(""Microsoft.Json.Schema.ToDotNet"", """ + VersionConstants.FileVersion + @""")]
    public sealed class FileDataEqualityComparer : IEqualityComparer<FileData>
    {
        public static readonly FileDataEqualityComparer Instance = new FileDataEqualityComparer();

        public bool Equals(FileData left, FileData right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
            {
                return false;
            }

            if (left.Path != right.Path)
            {
                return false;
            }

            return true;
        }

        public int GetHashCode(FileData obj)
        {
            if (ReferenceEquals(obj, null))
            {
                return 0;
            }

            int result = 17;
            unchecked
            {
                if (obj.Path != null)
                {
                    result = (result * 31) + obj.Path.GetHashCode();
                }
            }

            return result;
        }
    }
}"
)
        };

        [Theory(DisplayName = nameof(ClassNameHint))]
        [MemberData(nameof(TestCases))]
        public void ClassNameHint(TestCase test)
        {
            _settings.GenerateEqualityComparers = true;
            _settings.HintDictionary = new HintDictionary(test.HintsText);
            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);

            JsonSchema schema = SchemaReader.ReadSchema(test.SchemaText);

            generator.Generate(schema);

            var expectedContentsDictionary = new Dictionary<string, ExpectedContents>
            {
                [_settings.RootClassName] = new ExpectedContents
                {
                    ClassContents = test.PrimaryClassText,
                    ComparerClassContents = test.PrimaryClassComparerText
                },
                [test.HintedClassName] = new ExpectedContents
                {
                    ClassContents = test.HintedClassText,
                    ComparerClassContents = test.HintedClassComparerText
                }
            };

            Assert.FileContentsMatchExpectedContents(_testFileSystem, expectedContentsDictionary);
        }
    }
}
