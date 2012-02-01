﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using FLEx_ChorusPlugin.Infrastructure;
using FLEx_ChorusPlugin.Infrastructure.DomainServices;

namespace FLEx_ChorusPlugin.Contexts.Scripture
{
	internal static class ScriptureBoundedContextService
	{
		internal static void NestContext(XElement languageProjectElement, XElement scriptureElement,
										 XmlReaderSettings readerSettings, string baseDirectory,
										 IDictionary<string, SortedDictionary<string, XElement>> classData,
										 Dictionary<string, string> guidToClassMapping,
										 HashSet<string> skipWriteEmptyClassFiles)
		{
			// baseDirectory is root/Scripture and has already been created by caller.
			var scriptureBaseDir = baseDirectory;

			CmObjectNestingService.NestObject(false, scriptureElement,
				new Dictionary<string, HashSet<string>>(),
				classData,
				guidToClassMapping);

			// Remove 'ownerguid'.
			scriptureElement.Attribute(SharedConstants.OwnerGuid).Remove();

			FileWriterService.WriteNestedFile(
				Path.Combine(scriptureBaseDir, SharedConstants.ScriptureTransFilename),
				readerSettings,
				new XDocument(new XDeclaration("1.0", "utf-8", "yes"),
					new XElement(SharedConstants.TranslatedScripture, scriptureElement)));

			languageProjectElement.Element(SharedConstants.TranslatedScripture).RemoveNodes();

			ObjectFinderServices.ProcessLists(classData, skipWriteEmptyClassFiles, new HashSet<string> {
				SharedConstants.Scripture,
				"ScrBook", "ScrSection", "ScrTxtPara", "ScrFootnote", "ScrDifference",
				"ScrImportSet", "ScrImportSource", "ScrImportP6Project", "ScrImportSFFiles", "ScrMarkerMapping",
				"ScrBookAnnotations", "ScrScriptureNote", "ScrCheckRun" });
		}

		internal static void FlattenContext(
			SortedDictionary<string, XElement> highLevelData,
			SortedDictionary<string, XElement> sortedData,
			string scriptureBaseDir)
		{
			if (!Directory.Exists(scriptureBaseDir))
				return;

			// scriptureBaseDir is root/Scripture.
			var doc = XDocument.Load(Path.Combine(scriptureBaseDir, SharedConstants.ScriptureTransFilename));
			var scrElement = doc.Element(SharedConstants.TranslatedScripture).Elements().First();

			// Owned by LangProj in TranslatedScripture prop.
			var langProjElement = highLevelData["LangProject"];
			CmObjectFlatteningService.RestoreObjsurElement(langProjElement, SharedConstants.TranslatedScripture, scrElement);

			CmObjectFlatteningService.FlattenObject(sortedData,
				scrElement,
				langProjElement.Attribute(SharedConstants.GuidStr).Value.ToLowerInvariant()); // Restore 'ownerguid' to scrElement.

			highLevelData.Add(scrElement.Attribute(SharedConstants.Class).Value, scrElement);
		}

		internal static void RemoveBoundedContextData(string scriptureBaseDir)
		{
			// baseDirectory is root/Scripture.
			if (!Directory.Exists(scriptureBaseDir))
				return;

			const string transScripPathname = SharedConstants.ScriptureTransFilename;
			if (File.Exists(transScripPathname))
				File.Delete(transScripPathname);

			// Scripture domain does it all.
			// FileWriterService.RemoveEmptyFolders(scriptureBaseDir, true);
		}
	}
}