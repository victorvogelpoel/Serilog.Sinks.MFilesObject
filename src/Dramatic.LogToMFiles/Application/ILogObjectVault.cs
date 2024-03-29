﻿// ILogObjectVault.cs
// 30-1-2022
// Copyright 2022 Dramatic Development - Victor Vogelpoel
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
using System.Collections.Generic;
using MFilesAPI;

namespace Dramatic.LogToMFiles
{
    public interface ILogObjectVault
    {
        bool IsLogObjectStructurePresent();

        List<ObjVer> SearchLogObjects(string logObjectBaseName);

        string ReadLogMessageFromLogObject(ObjVer logObjVer);

        bool WriteLogMessageToExistingLogObject(ObjVer logObjVer, string logMessage);
        bool WriteLogMessageToNewLogObject(string newLogObjectName, string logMessage);
    }
}
