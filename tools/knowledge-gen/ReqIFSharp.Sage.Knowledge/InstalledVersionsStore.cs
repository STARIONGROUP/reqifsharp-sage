// -------------------------------------------------------------------------------------------------
// <copyright file="InstalledVersionsStore.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.Knowledge
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text.Json;

    /// <summary>
    /// Reads and writes <c>knowledge/installed.json</c>.
    /// </summary>
    public sealed class InstalledVersionsStore
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private readonly string manifestPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="InstalledVersionsStore"/> class.
        /// </summary>
        /// <param name="knowledgeRootDirectory">The <c>knowledge/</c> directory.</param>
        public InstalledVersionsStore(string knowledgeRootDirectory)
        {
            this.manifestPath = Path.Combine(knowledgeRootDirectory, "installed.json");
        }

        /// <summary>
        /// Loads the manifest, or an empty one if it does not exist yet.
        /// </summary>
        /// <returns>The manifest.</returns>
        public InstalledVersionsManifest Load()
        {
            if (!File.Exists(this.manifestPath))
            {
                return InstalledVersionsManifest.Empty;
            }

            var manifest = JsonSerializer.Deserialize<InstalledVersionsManifest>(File.ReadAllText(this.manifestPath), SerializerOptions) ?? InstalledVersionsManifest.Empty;

            return manifest with
            {
                Versions = manifest.Versions ?? [],
                Guides = manifest.Guides ?? [],
            };
        }

        /// <summary>
        /// Writes the manifest.
        /// </summary>
        /// <param name="manifest">The manifest.</param>
        public void Save(InstalledVersionsManifest manifest)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(this.manifestPath)!);
            File.WriteAllText(this.manifestPath, JsonSerializer.Serialize(manifest, SerializerOptions));
        }

        /// <summary>
        /// Records that a ReqIF version's sources are stored; it becomes the default when
        /// <paramref name="setAsDefault"/> is true or no default is set yet.
        /// </summary>
        /// <param name="version">The ReqIF version.</param>
        /// <param name="setAsDefault">Whether to make it the default.</param>
        public void MarkFetched(string version, bool setAsDefault)
        {
            var manifest = this.Load();
            var updated = UpsertVersion(manifest, version, entry => entry with { Fetched = true });
            this.Save(updated with { Default = setAsDefault || manifest.Default is null ? version : manifest.Default });
        }

        /// <summary>
        /// Records that a ReqIF version's knowledge is generated. <paramref name="specGenerated"/> is sticky:
        /// a later failed extraction does not clear it.
        /// </summary>
        /// <param name="version">The ReqIF version.</param>
        /// <param name="specGenerated">Whether the specification text was extracted in this run.</param>
        public void MarkGenerated(string version, bool specGenerated)
        {
            var manifest = this.Load();
            this.Save(UpsertVersion(manifest, version, entry => entry with { Generated = true, SpecGenerated = entry.SpecGenerated || specGenerated }));
        }

        /// <summary>
        /// Records that an implementation guide version is imported.
        /// </summary>
        /// <param name="version">The guide version.</param>
        public void MarkGuideImported(string version)
        {
            var manifest = this.Load();
            this.Save(UpsertGuide(manifest, version, entry => entry with { Imported = true }));
        }

        /// <summary>
        /// Records whether an implementation guide's clause text is extracted (sticky, like
        /// <see cref="MarkGenerated"/>).
        /// </summary>
        /// <param name="version">The guide version.</param>
        /// <param name="succeeded">Whether the extraction succeeded in this run.</param>
        public void MarkGuideGenerated(string version, bool succeeded)
        {
            var manifest = this.Load();
            this.Save(UpsertGuide(manifest, version, entry => entry with { Generated = entry.Generated || succeeded }));
        }

        /// <summary>
        /// Records the generated reqifsharp API reference.
        /// </summary>
        /// <param name="api">The package versions it describes.</param>
        public void MarkApiGenerated(InstalledApi api)
        {
            this.Save(this.Load() with { Api = api });
        }

        /// <summary>
        /// Sets the default ReqIF version.
        /// </summary>
        /// <param name="version">The ReqIF version.</param>
        /// <exception cref="InvalidOperationException">The version has not been generated.</exception>
        public void SetDefault(string version)
        {
            var manifest = this.Load();
            var entry = manifest.Versions.FirstOrDefault(v => v.Version == version);
            if (entry is null || !entry.Generated)
            {
                throw new InvalidOperationException($"ReqIF {version} has not been generated yet - run 'generate --version {version}' first.");
            }

            this.Save(manifest with { Default = version });
        }

        /// <summary>
        /// Removes a ReqIF version's entry.
        /// </summary>
        /// <param name="version">The ReqIF version.</param>
        public void Remove(string version)
        {
            var manifest = this.Load();
            var remaining = manifest.Versions.Where(v => v.Version != version).ToList();
            var newDefault = manifest.Default == version ? remaining.FirstOrDefault()?.Version : manifest.Default;
            this.Save(manifest with { Versions = remaining, Default = newDefault });
        }

        /// <summary>
        /// Removes an implementation guide's entry.
        /// </summary>
        /// <param name="version">The guide version.</param>
        public void RemoveGuide(string version)
        {
            var manifest = this.Load();
            this.Save(manifest with { Guides = manifest.Guides.Where(g => g.Version != version).ToList() });
        }

        private static InstalledVersionsManifest UpsertVersion(InstalledVersionsManifest manifest, string version, Func<InstalledVersion, InstalledVersion> update)
        {
            var existing = manifest.Versions.FirstOrDefault(v => v.Version == version);
            var updatedEntry = update(existing ?? new InstalledVersion(version, false, false, false));
            var versions = manifest.Versions.Where(v => v.Version != version).Append(updatedEntry).OrderBy(v => v.Version, StringComparer.Ordinal).ToList();
            return manifest with { Versions = versions };
        }

        private static InstalledVersionsManifest UpsertGuide(InstalledVersionsManifest manifest, string version, Func<InstalledGuide, InstalledGuide> update)
        {
            var existing = manifest.Guides.FirstOrDefault(g => g.Version == version);
            var updatedEntry = update(existing ?? new InstalledGuide(version, false, false));
            var guides = manifest.Guides.Where(g => g.Version != version).Append(updatedEntry).OrderBy(g => g.Version, StringComparer.Ordinal).ToList();
            return manifest with { Guides = guides };
        }
    }
}
