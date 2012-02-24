﻿using System.Xml;
using FLEx_ChorusPlugin.Properties;

namespace FLEx_ChorusPlugin.Infrastructure.Handling
{
	class PossibilityContextGenerator : FieldWorkObjectContextGenerator
	{
		protected override string GetLabel(XmlNode start)
		{
			return GetLabelForPossibilityItem(start);
		}


		string ListItemLabel
		{
			get { return Resources.kPossibilityItemLabel; }
		}

		private string GetLabelForPossibilityItem(XmlNode possibility)
		{
			var itemName = UnidentifiableLabel;
			var listName = ListLabel + " " + UnidentifiableLabel;

			if (possibility != null)
			{
				itemName = GetNameOrAbbreviation(possibility);

				if (possibility.ParentNode != null)
					listName = base.GetLabel(possibility.SelectSingleNode("ancestor::CmPossibilityList"));
			}
			return ListItemLabel + " '" + itemName + "' from " + listName;
		}
	}
}
