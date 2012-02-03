﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using FLEx_ChorusPlugin.Infrastructure;
using FLEx_ChorusPlugin.Infrastructure.DomainServices;

namespace FLEx_ChorusPlugin.Contexts.Linguistics.Reversals
{
	/// <summary>
	/// Read/Write/Delete the Reversal bounded context.
	///
	/// The Reversal Index instances, including all they own, need to then be removed from 'classData',
	/// as that stuff will be stored elsewhere.
	///
	/// Each ReversalIndex instance will be in its own file, along with everything it owns (nested ownership as well).
	/// The pattern is:
	/// Linguistics\Reversals\foo.reversal, where foo.reversal is the Reversal Index file and 'foo' is the WritingSystem property of the ReversalIndex.
	///
	/// The output file for each will be:
	/// <reversal>
	///		<ReversalIndex>
	/// 1. The "Entries" element's contents will be relocated after the "ReversalIndex" element.
	/// 2. All other owned stuff will be nested here.
	///		</ReversalIndex>
	///		<ReversalInxEntry>Nested for what they own.</ReversalInxEntry>
	///		...
	///		<ReversalInxEntry>Nested for what they own.</ReversalInxEntry>
	/// </reversal>
	/// </summary>
	internal static class ReversalBoundedContextService
	{
		private const string ReversalRootFolder = "Reversals";

		internal static void NestContext(string linguisticsBaseDir,
			IDictionary<string, SortedDictionary<string, XElement>> classData,
			Dictionary<string, string> guidToClassMapping)
		{
			var allLexDbs = classData["LexDb"].FirstOrDefault();
			if (allLexDbs.Value == null)
				return; // No LexDb, then there can be no reversals.

			SortedDictionary<string, XElement> sortedInstanceData = classData["ReversalIndex"];
			if (sortedInstanceData.Count == 0)
				return; // no reversals, as in Lela-Teli-3.

			var lexDb = allLexDbs.Value;
			lexDb.Element("ReversalIndexes").RemoveNodes(); // Restored in FlattenContext method.

			var reversalDir = Path.Combine(linguisticsBaseDir, ReversalRootFolder);
			if (!Directory.Exists(reversalDir))
				Directory.CreateDirectory(reversalDir);

			var srcDataCopy = new SortedDictionary<string, XElement>(sortedInstanceData);
			foreach (var reversalIndexKvp in srcDataCopy)
			{
				var revIndex = reversalIndexKvp.Value;

				var ws = revIndex.Element("WritingSystem").Element("Uni").Value;
				var reversalFilename = ws + ".reversal";

				CmObjectNestingService.NestObject(false, revIndex,
					new Dictionary<string, HashSet<string>>(),
					classData,
					guidToClassMapping);

				var entriesElement = revIndex.Element("Entries");
				var root = new XElement("Reversal",
					new XElement(SharedConstants.Header, revIndex));
				if (entriesElement == null || !entriesElement.Elements().Any())
				{
					// Add dummy entry, so FastXmlSplitter will have something to work with.
					root.Add(new XElement("ReversalIndexEntry",
												   new XAttribute(SharedConstants.GuidStr, Guid.Empty.ToString().ToLowerInvariant())));
				}
				else
				{
					root.Add(entriesElement.Elements()); // NB: These were already sorted, way up in MultipleFileServices::CacheDataRecord, since "Entries" is a collection prop.
					entriesElement.RemoveNodes();
				}

				FileWriterService.WriteNestedFile(Path.Combine(reversalDir, reversalFilename), root);
			}
		}

		internal static void FlattenContext(
			SortedDictionary<string, XElement> highLevelData,
			SortedDictionary<string, XElement> sortedData,
			string linguisticsBaseDir)
		{
			var reversalDir = Path.Combine(linguisticsBaseDir, ReversalRootFolder);
			if (!Directory.Exists(reversalDir))
				return;

			var lexDb = highLevelData["LexDb"];
			var sortedRevs = new SortedDictionary<string, XElement>(StringComparer.OrdinalIgnoreCase);
			foreach (var reversalPathname in Directory.GetFiles(reversalDir, "*.reversal", SearchOption.TopDirectoryOnly))
			{
				var reversalDoc = XDocument.Load(reversalPathname);
				// Put entries back into index's Entries element.
				var root = reversalDoc.Element("Reversal");
				var header = root.Element(SharedConstants.Header);
				var revIdx = header.Element("ReversalIndex");
				// Put all records back in ReversalIndex, before sort and restore.
				// EXCEPT, if there is only one of them and it is guid.Empty, then skip it
				var records = root.Elements("ReversalIndexEntry").ToList();
				if (records.Count > 1 || records[0].Attribute(SharedConstants.GuidStr).Value.ToLowerInvariant() != Guid.Empty.ToString().ToLowerInvariant())
					revIdx.Element("Entries").Add(records); // NB: These full objects will be turned into regular objsur elements in the flattening process.
				CmObjectFlatteningService.FlattenObject(reversalPathname, sortedData, revIdx, lexDb.Attribute(SharedConstants.GuidStr).Value.ToLowerInvariant()); // Restore 'ownerguid' to indices.
				var revIdxGuid = revIdx.Attribute(SharedConstants.GuidStr).Value.ToLowerInvariant();
				sortedRevs.Add(revIdxGuid, BaseDomainServices.CreateObjSurElement(revIdxGuid));
			}

			// Restore lexDb ReversalIndexes property in sorted order.
			if (sortedRevs.Count == 0)
				return;
			var reversalsOwningProp = lexDb.Element("ReversalIndexes");
			foreach (var sortedRev in sortedRevs.Values)
				reversalsOwningProp.Add(sortedRev);
		}

		internal static void RemoveBoundedContextData(string linguisticsBaseDir)
		{
			var reversalDir = Path.Combine(linguisticsBaseDir, ReversalRootFolder);
			if (!Directory.Exists(reversalDir))
				return;

			foreach (var reversalPathname in Directory.GetFiles(reversalDir, "*.reversal", SearchOption.TopDirectoryOnly))
				File.Delete(reversalPathname);

			// Linguistics domain will call this.
			// FileWriterService.RemoveEmptyFolders(reversalDir, true);
		}
	}
}
