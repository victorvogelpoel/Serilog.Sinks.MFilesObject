// GlobalSuppressions.cs
// 13-9-2021
// Copyright 2021 Dramatic Development - Victor Vogelpoel
// If this works, it was written by Victor Vogelpoel (victor@victorvogelpoel.nl).
// If it doesn't, I don't know who wrote it.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>", Scope = "member", Target = "~M:DemoVault.AddLoggingStructure.Program.Main(System.String[])")]
[assembly: SuppressMessage("Minor Code Smell", "S3247:Duplicate casts should not be made", Justification = "<Pending>", Scope = "member", Target = "~M:DemoVault.AddLoggingStructure.ExceptionExtensions.AppendCollection(System.Text.StringBuilder,System.String,System.Collections.IEnumerable,DemoVault.AddLoggingStructure.ExceptionOptions)")]
[assembly: SuppressMessage("Style", "IDE0020:Use pattern matching", Justification = "<Pending>", Scope = "member", Target = "~M:DemoVault.AddLoggingStructure.ExceptionExtensions.AppendValue(System.Text.StringBuilder,System.String,System.Object,DemoVault.AddLoggingStructure.ExceptionOptions)")]
[assembly: SuppressMessage("Style", "IDE0020:Use pattern matching", Justification = "<Pending>", Scope = "member", Target = "~M:DemoVault.AddLoggingStructure.ExceptionExtensions.AppendCollection(System.Text.StringBuilder,System.String,System.Collections.IEnumerable,DemoVault.AddLoggingStructure.ExceptionOptions)")]
[assembly: SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "<Pending>", Scope = "type", Target = "~T:DemoVault.AddLoggingStructure.ExceptionOptions")]
[assembly: SuppressMessage("Minor Code Smell", "S3247:Duplicate casts should not be made", Justification = "<Pending>", Scope = "member", Target = "~M:DemoVault.AddLoggingStructure.ExceptionExtensions.AppendValue(System.Text.StringBuilder,System.String,System.Object,DemoVault.AddLoggingStructure.ExceptionOptions)")]
[assembly: SuppressMessage("Style", "IDE0038:Use pattern matching", Justification = "<Pending>", Scope = "member", Target = "~M:DemoVault.AddLoggingStructure.ExceptionExtensions.AppendValue(System.Text.StringBuilder,System.String,System.Object,DemoVault.AddLoggingStructure.ExceptionOptions)")]
[assembly: SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed", Justification = "<Pending>", Scope = "member", Target = "~M:DemoVault.AddLoggingStructure.ExceptionExtensions.IndentString(System.String,DemoVault.AddLoggingStructure.ExceptionOptions)~System.String")]
[assembly: SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "<Pending>", Scope = "member", Target = "~M:DemoVault.AddLoggingStructure.ExceptionExtensions.IndentString(System.String,DemoVault.AddLoggingStructure.ExceptionOptions)~System.String")]
