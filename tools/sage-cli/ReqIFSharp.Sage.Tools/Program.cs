// -------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
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
