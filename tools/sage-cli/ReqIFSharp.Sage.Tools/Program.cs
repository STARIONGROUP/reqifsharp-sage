// -------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Starion Group S.A.">
//
//   Copyright (C) 2026 Starion Group S.A.
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
//
// </copyright>
// ------------------------------------------------------------------------------------------------

using System;
using System.CommandLine;
using System.Text;

using ReqIFSharp.Sage.Tools.Commands;

Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

var root = new RootCommand("reqifsharp-sage: fetch, generate and query the OMG ReqIF 1.2 / reqifsharp knowledge base, and inspect .reqif/.reqifz files.");
root.Add(VersionsCommand.Build());
root.Add(FetchCommand.Build());
root.Add(ImportGuideCommand.Build());
root.Add(GenerateCommand.Build());
root.Add(UseCommand.Build());
root.Add(RemoveCommand.Build());
root.Add(CheckCommand.Build());
root.Add(InspectCommand.Build());

return await root.Parse(args).InvokeAsync();
