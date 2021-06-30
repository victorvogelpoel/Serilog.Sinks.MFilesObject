﻿// GlobalSuppressions.cs
// 14-5-2021
// Copyright 2021 Dramatic Development - Victor Vogelpoel
// If this works, it was written by Victor Vogelpoel (victor@victorvogelpoel.nl).
// If it doesn't, I don't know who wrote it.
//

// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Minor Code Smell", "S2737:\"catch\" clauses should do more than rethrow", Justification = "<Pending>", Scope = "member", Target = "~M:Serilog.Sinks.MFilesObject.MFilesLogObjectMessageSink.EmitBatchAsync(System.Collections.Generic.IEnumerable{Serilog.Events.LogEvent})~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Major Code Smell", "S125:Sections of code should not be commented out", Justification = "<Pending>", Scope = "member", Target = "~M:Serilog.Sinks.MFilesObject.MFilesLogObjectMessageSink.OnEmptyBatchAsync~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Major Code Smell", "S125:Sections of code should not be commented out", Justification = "<Pending>", Scope = "type", Target = "~T:Serilog.Sinks.MFilesObject.MFilesLogObjectMessageSink")]
