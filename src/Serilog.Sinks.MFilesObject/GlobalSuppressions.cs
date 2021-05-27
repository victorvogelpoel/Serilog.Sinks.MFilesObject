// GlobalSuppressions.cs
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

[assembly: SuppressMessage("Security", "SCS0005:Weak random generator", Justification = "<Pending>", Scope = "member", Target = "~M:Serilog.Sinks.MFilesObject.MFilesObjectLogSink.EmitToMFilesLogObject(System.String)")]
[assembly: SuppressMessage("Critical Code Smell", "S927:Parameter names should match base declaration and other partial definitions", Justification = "<Pending>", Scope = "member", Target = "~M:Serilog.Sinks.MFilesObject.LogObjectCreatedComparer.Compare(MFilesAPI.ObjectVersion,MFilesAPI.ObjectVersion)~System.Int32")]
[assembly: SuppressMessage("Security", "SCS0005:Weak random generator", Justification = "<Pending>", Scope = "member", Target = "~M:Serilog.Sinks.MFilesObject.MFilesLogRepository.IsObjectCheckedOut(MFilesAPI.ObjID,System.Int32)~System.Boolean")]
[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>", Scope = "member", Target = "~M:Serilog.Sinks.MFilesObject.MFilesLogRepository.WriteLogMessage(System.String)")]
[assembly: SuppressMessage("Minor Code Smell", "S2737:\"catch\" clauses should do more than rethrow", Justification = "<Pending>", Scope = "member", Target = "~M:Serilog.Sinks.MFilesObject.MFilesObjectLogSink.EmitBatchAsync(System.Collections.Generic.IEnumerable{Serilog.Events.LogEvent})~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Major Code Smell", "S125:Sections of code should not be commented out", Justification = "<Pending>", Scope = "member", Target = "~M:Serilog.Sinks.MFilesObject.MFilesObjectLogSink.OnEmptyBatchAsync~System.Threading.Tasks.Task")]
