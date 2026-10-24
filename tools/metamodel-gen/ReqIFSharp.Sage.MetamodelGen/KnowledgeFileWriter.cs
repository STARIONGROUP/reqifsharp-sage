// -------------------------------------------------------------------------------------------------
// <copyright file="KnowledgeFileWriter.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.MetamodelGen
{
    using System.IO;
    using System.Text;
    using System.Text.Encodings.Web;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Writes generated knowledge files deterministically: UTF-8 without a byte-order mark, LF line
    /// endings, a trailing newline, and fixed JSON serializer options.
    /// </summary>
    public static class KnowledgeFileWriter
    {
        /// <summary>
        /// The JSON options used for every generated JSON file: indented, camelCase, nulls written, and
        /// no escaping of <c>&lt;</c>/<c>&gt;</c>/<c>&amp;</c> so XML names stay readable.
        /// </summary>
        public static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        private static readonly UTF8Encoding Utf8WithoutByteOrderMark = new(encoderShouldEmitUTF8Identifier: false);

        /// <summary>
        /// Writes <paramref name="content"/> to <paramref name="path"/>, creating the directory if needed.
        /// </summary>
        /// <param name="path">The target path.</param>
        /// <param name="content">The text to write.</param>
        public static void WriteText(string path, string content)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);

            var normalized = content.Replace("\r\n", "\n");
            if (!normalized.EndsWith('\n'))
            {
                normalized += "\n";
            }

            File.WriteAllText(path, normalized, Utf8WithoutByteOrderMark);
        }

        /// <summary>
        /// Serializes <paramref name="value"/> with <see cref="JsonOptions"/> and writes it to <paramref name="path"/>.
        /// </summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="path">The target path.</param>
        /// <param name="value">The value to serialize.</param>
        public static void WriteJson<T>(string path, T value)
        {
            WriteText(path, JsonSerializer.Serialize(value, JsonOptions));
        }
    }
}
