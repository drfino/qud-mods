using System.Collections.Generic;
using System.Linq;
using XRL.UI;
using Qud.UI;
using ConsoleLib.Console;
using Plaidman.AnEyeForValue.Utils;

namespace Plaidman.AnEyeForValue.Menus {
	public class InventoryPopup {
		public SortType CurrentSortType;
		private Dictionary<SortType, InventoryItem[]> ItemListCache;
		
		private void ResetCache() {
			ItemListCache = new() {
				{ SortType.Value, null },
				{ SortType.Weight, null },
			};
		}
		
		private InventoryItem[] SortItems(InventoryItem[] items) {
			var cache = ItemListCache.GetValue(CurrentSortType);
			
			if (cache == null) {
				var comparer = PopupUtils.Comparers.GetValue(CurrentSortType);
				cache = items.OrderBy(item => item, comparer).ToArray();
				ItemListCache.Set(CurrentSortType, cache);
			}
			
			return cache;
		}

		public int[] ShowPopup(InventoryItem[] options) {
			var defaultSelected = 0;
			var weightSelected = 0;
			var selectedItems = new HashSet<int>();
			
			ResetCache();
			var sortedOptions = SortItems(options);
			IRenderable[] itemIcons = sortedOptions.Select((item) => { return item.Icon; }).ToArray();
			string[] itemLabels = sortedOptions.Select((item) => {
				var selected = selectedItems.Contains(item.Index);
				return PopupUtils.GetItemLabel(selected, item, CurrentSortType);
			}).ToArray();

			QudMenuItem[] menuCommands = new QudMenuItem[2]
			{
				new() {
					text = "{{W|[D]}} {{y|Drop Items}}",
					command = "option:-2",
					hotkey = "D"
				},
				new() {
					text = PopupUtils.GetSortLabel(CurrentSortType),
					command = "option:-3",
					hotkey = "Tab"
				},
			};

			while (true) {
				var intro = "Mark items here, press 'd' to drop them.\n";
				intro += "Selected Item Weight: {{w|" + weightSelected + "#}}\n\n";
				
				int selectedIndex = Popup.PickOption(
					Title: "Inventory Items",
					Intro: intro,
					IntroIcon: null,
					Options: itemLabels,
					RespectOptionNewlines: false,
					Icons: itemIcons,
					DefaultSelected: defaultSelected,
					Buttons: menuCommands,
					AllowEscape: true
				);

				switch (selectedIndex) {
					case -1:  // Esc / Cancelled
						return null;

					case -2: // D drop items
						return selectedItems.ToArray();

					default:
						break;
				}
				
				if (selectedIndex == -3) {
					CurrentSortType = PopupUtils.NextSortType.GetValue(CurrentSortType);

					menuCommands[1].text = PopupUtils.GetSortLabel(CurrentSortType);
					sortedOptions = SortItems(options);
					itemIcons = sortedOptions.Select((item) => { return item.Icon; }).ToArray();
					itemLabels = sortedOptions.Select((item) => {
						var selected = selectedItems.Contains(item.Index);
						return PopupUtils.GetItemLabel(selected, item, CurrentSortType);
					}).ToArray();
					
					continue;
				}

				var mappedItem = sortedOptions[selectedIndex];
				if (selectedItems.Contains(mappedItem.Index)) {
					selectedItems.Remove(mappedItem.Index);
					weightSelected -= mappedItem.Weight;
					itemLabels[selectedIndex] = PopupUtils.GetItemLabel(false, mappedItem, CurrentSortType);
				} else {
					selectedItems.Add(mappedItem.Index);
					weightSelected += mappedItem.Weight;
					itemLabels[selectedIndex] = PopupUtils.GetItemLabel(true, mappedItem, CurrentSortType);
				}

				defaultSelected = selectedIndex;
			}
		}
	}
};
