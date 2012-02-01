﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using FLEx_ChorusPlugin.Infrastructure;
using FLEx_ChorusPlugin.Infrastructure.DomainServices;
using NUnit.Framework;

namespace FLEx_ChorusPluginTests.Infrastructure.DomainServices
{
	[TestFixture]
	public class CmObjectNestingServiceTests
	{
		private XElement _rt;
		private Dictionary<string, SortedDictionary<string, XElement>> _classData;
		private Dictionary<string, string> _guidToClassMapping;

		[SetUp]
		public void SetupTest()
		{
			const string revIdxGuid = "fe832a87-4846-4895-9c7e-98c5da0c84ba";
			_rt = new XElement(SharedConstants.RtTag,
								  new XAttribute(SharedConstants.Class, "ReversalIndex"),
								  new XAttribute(SharedConstants.GuidStr, revIdxGuid));
			_classData = new Dictionary<string, SortedDictionary<string, XElement>>
								{
									{
										"ReversalIndex",
										new SortedDictionary<string, XElement>
											{
												{revIdxGuid, _rt}
											}
									}
								};
			_guidToClassMapping = new Dictionary<string, string>
									{
										{ revIdxGuid, "ReversalIndex" }
									};
		}

		[TearDown]
		public void TearDownTest()
		{
			_rt = null;
			_classData = null;
			_guidToClassMapping = null;
		}

		[Test]
		public void NullObjectThrows()
		{
			Assert.Throws<ArgumentNullException>(() => CmObjectNestingService.NestObject(false, null,
				new Dictionary<string, HashSet<string>>(),
				new Dictionary<string, SortedDictionary<string, XElement>>(),
				new Dictionary<string, string>()));
		}

		[Test]
		public void NullExceptionListThrows()
		{
			Assert.Throws<ArgumentNullException>(() => CmObjectNestingService.NestObject(false, new XElement("junk"),
				null,
				new Dictionary<string, SortedDictionary<string, XElement>>(),
				new Dictionary<string, string>()));
		}

		[Test]
		public void NullClassDataThrows()
		{
			Assert.Throws<ArgumentNullException>(() => CmObjectNestingService.NestObject(false, new XElement("junk"),
				new Dictionary<string, HashSet<string>>(),
				null,
				new Dictionary<string, string>()));
		}

		[Test]
		public void NullGuidToClassMappingThrows()
		{
			Assert.Throws<ArgumentNullException>(() => CmObjectNestingService.NestObject(false, new XElement("junk"),
				new Dictionary<string, HashSet<string>>(),
				new Dictionary<string, SortedDictionary<string, XElement>>(),
				null));
		}

		[Test]
		public void ElementRenamed()
		{
			CmObjectNestingService.NestObject(false, _rt,
											  new Dictionary<string, HashSet<string>>(),
											  _classData,
											  _guidToClassMapping);
			Assert.IsTrue(_rt.Name.LocalName == "ReversalIndex");
			Assert.IsNull(_rt.Attribute(SharedConstants.Class));
		}

		[Test]
		public void OwnedObjectsAreNested()
		{
			AddOwnedObjects();
			CmObjectNestingService.NestObject(false, _rt,
				new Dictionary<string, HashSet<string>>(),
				_classData,
				_guidToClassMapping);
			var entriesElement = _rt.Element("Entries");
			var entriesElements = entriesElement.Elements("ReversalIndexEntry");
			Assert.AreEqual(2, entriesElements.Count());
			entriesElements = _rt.Element("PartsOfSpeech").Elements("CmPossibilityList");
			Assert.AreEqual(1, entriesElements.Count());
			entriesElements = entriesElements.ToList()[0].Element("Possibilities").Elements(SharedConstants.Ownseq);
			Assert.AreEqual(2, entriesElements.Count());
			var pos = entriesElements.ToList()[0];
			Assert.AreEqual(pos.Attribute(SharedConstants.GuidStr).Value, "c1ed6dc6-e382-11de-8a39-0800200c9a66");
			Assert.AreEqual(pos.Attribute(SharedConstants.Class).Value, "PartOfSpeech");
			pos = entriesElements.ToList()[1];
			Assert.AreEqual(pos.Attribute(SharedConstants.GuidStr).Value, "c1ed6dc7-e382-11de-8a39-0800200c9a66");
			Assert.AreEqual(pos.Attribute(SharedConstants.Class).Value, "PartOfSpeech");
		}

		[Test]
		public void ExcludedObjectsAreNotNested()
		{
			var exclusions = new Dictionary<string, HashSet<string>>
								{
									{"ReversalIndex", new HashSet<string> {"PartsOfSpeech"}}
								};
			AddOwnedObjects();
			CmObjectNestingService.NestObject(false, _rt,
				exclusions,
				_classData,
				_guidToClassMapping);
			var entriesElement = _rt.Element("Entries");
			var entriesElements = entriesElement.Elements("ReversalIndexEntry");
			Assert.AreEqual(2, entriesElements.Count());
			Assert.AreEqual(0, _rt.Element("PartsOfSpeech").Elements("CmPossibilityList").Count());
			Assert.AreEqual(1, _rt.Element("PartsOfSpeech").Elements(SharedConstants.Objsur).Count());
		}

		[Test]
		public void RefSeqPropertiesAreChangedToRefseq()
		{
			var rt = new XElement(SharedConstants.RtTag,
								  new XAttribute(SharedConstants.Class, "Segment"),
								  new XAttribute(SharedConstants.GuidStr, "c1ed6dc8-e382-11de-8a39-0800200c9a66"),
								  new XElement("Analyses",
									  new XElement(SharedConstants.Objsur,
												  new XAttribute(SharedConstants.GuidStr, "0039739a-7fcf-4838-8b75-566b8815a29f"),
												  new XAttribute("t", "r")),
										new XElement(SharedConstants.Objsur,
												  new XAttribute(SharedConstants.GuidStr, "00b560a2-9af0-4185-bbeb-c0eb3c5e3769"),
												  new XAttribute("t", "r"))));
			var classData = new Dictionary<string, SortedDictionary<string, XElement>>();
			var data = new SortedDictionary<string, XElement>
						{
							{"c1ed6dc8-e382-11de-8a39-0800200c9a66", rt}
						};
			classData.Add("Segment", data);
			var guidToClassMapping = new Dictionary<string, string> {{"c1ed6dc8-e382-11de-8a39-0800200c9a66", "Segment"}};
			CmObjectNestingService.NestObject(false, rt,
				new Dictionary<string, HashSet<string>>(),
				classData,
				guidToClassMapping);
			var result = rt.ToString();
			Assert.IsTrue(result.Contains(SharedConstants.Refseq));
		}

		private void AddOwnedObjects()
		{
			// Add two entries, the first one having a nested entry.
			var rtGuid = _rt.Attribute(SharedConstants.GuidStr).Value;
			var data = new SortedDictionary<string, XElement>();
			var entry1 = new XElement(SharedConstants.RtTag,
									  new XAttribute(SharedConstants.Class, "ReversalIndexEntry"),
									  new XAttribute(SharedConstants.GuidStr, "0039739a-7fcf-4838-8b75-566b8815a29f"),
									  new XAttribute(SharedConstants.OwnerGuid, rtGuid));
			data.Add("0039739a-7fcf-4838-8b75-566b8815a29f", entry1);
			_guidToClassMapping.Add("0039739a-7fcf-4838-8b75-566b8815a29f", "ReversalIndexEntry");
			var subentry1 = new XElement(SharedConstants.RtTag,
									  new XAttribute(SharedConstants.Class, "ReversalIndexEntry"),
									  new XAttribute(SharedConstants.GuidStr, "14a6b4bc-1bb3-4c67-b70c-5a195e411e27"),
									  new XAttribute(SharedConstants.OwnerGuid, "0039739a-7fcf-4838-8b75-566b8815a29f"));
			data.Add("14a6b4bc-1bb3-4c67-b70c-5a195e411e27", subentry1);
			_guidToClassMapping.Add("14a6b4bc-1bb3-4c67-b70c-5a195e411e27", "ReversalIndexEntry");
			var entry2 = new XElement(SharedConstants.RtTag,
									  new XAttribute(SharedConstants.Class, "ReversalIndexEntry"),
									  new XAttribute(SharedConstants.GuidStr, "00b560a2-9af0-4185-bbeb-c0eb3c5e3769"),
									  new XAttribute(SharedConstants.OwnerGuid, rtGuid));
			data.Add("00b560a2-9af0-4185-bbeb-c0eb3c5e3769", entry2);
			_guidToClassMapping.Add("00b560a2-9af0-4185-bbeb-c0eb3c5e3769", "ReversalIndexEntry");
			_classData.Add("ReversalIndexEntry", data);
			var entriesElement = new XElement("Entries",
											  new XElement(SharedConstants.Objsur,
												  new XAttribute(SharedConstants.GuidStr, "0039739a-7fcf-4838-8b75-566b8815a29f"),
												  new XAttribute("t", "o")),
											  new XElement(SharedConstants.Objsur,
												  new XAttribute(SharedConstants.GuidStr, "00b560a2-9af0-4185-bbeb-c0eb3c5e3769"),
												  new XAttribute("t", "o")));
			_rt.Add(entriesElement);
			entriesElement = new XElement("Subentries",
											  new XElement(SharedConstants.Objsur,
												  new XAttribute(SharedConstants.GuidStr, "14a6b4bc-1bb3-4c67-b70c-5a195e411e27"),
												  new XAttribute("t", "o")));
			entry1.Add(entriesElement);

			// Add the POS list, with two possibilities (own-seq prop).
			const string posListGuid = "fb5e83e5-6576-455d-aba0-0b7a722b9b5d";
			var posList = new XElement(SharedConstants.RtTag,
									  new XAttribute(SharedConstants.Class, "CmPossibilityList"),
									  new XAttribute(SharedConstants.GuidStr, posListGuid),
									  new XAttribute(SharedConstants.OwnerGuid, rtGuid));
			var pos1 = new XElement(SharedConstants.RtTag,
									new XAttribute(SharedConstants.Class, "PartOfSpeech"),
									new XAttribute(SharedConstants.GuidStr, "c1ed6dc6-e382-11de-8a39-0800200c9a66"),
									new XAttribute(SharedConstants.OwnerGuid, posListGuid),
									new XElement("DateCreated", new XAttribute("val", "created")));
			var pos2 = new XElement(SharedConstants.RtTag,
									new XAttribute(SharedConstants.Class, "PartOfSpeech"),
									new XAttribute(SharedConstants.GuidStr, "c1ed6dc7-e382-11de-8a39-0800200c9a66"),
									new XAttribute(SharedConstants.OwnerGuid, posListGuid),
									new XElement("DateCreated", new XAttribute("val", "created")));
			entriesElement = new XElement("Possibilities",
											  new XElement(SharedConstants.Objsur,
												  new XAttribute(SharedConstants.GuidStr, "c1ed6dc6-e382-11de-8a39-0800200c9a66"),
												  new XAttribute("t", "o")),
											  new XElement(SharedConstants.Objsur,
												  new XAttribute(SharedConstants.GuidStr, "c1ed6dc7-e382-11de-8a39-0800200c9a66"),
												  new XAttribute("t", "o")));
			_guidToClassMapping.Add("c1ed6dc6-e382-11de-8a39-0800200c9a66", "PartOfSpeech");
			_guidToClassMapping.Add("c1ed6dc7-e382-11de-8a39-0800200c9a66", "PartOfSpeech");
			data = new SortedDictionary<string, XElement>
					{
						{"c1ed6dc6-e382-11de-8a39-0800200c9a66", pos1},
						{"c1ed6dc7-e382-11de-8a39-0800200c9a66", pos2}
					};
			_classData.Add("PartOfSpeech", data);
			posList.Add(entriesElement);

			entriesElement = new XElement("PartsOfSpeech",
											  new XElement(SharedConstants.Objsur,
												  new XAttribute(SharedConstants.GuidStr, posListGuid),
												  new XAttribute("t", "o")));
			_guidToClassMapping.Add(posListGuid, "CmPossibilityList");
			data = new SortedDictionary<string, XElement>
					{
						{posListGuid, posList}
					};
			_classData.Add("CmPossibilityList", data);
			_rt.Add(entriesElement);
		}
	}
}