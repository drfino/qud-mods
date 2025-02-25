using System;
using System.Collections.Generic;
using System.Linq;
using XRL;
using XRL.Language;
using XRL.UI;
using XRL.World;
using XRL.World.Parts;

namespace Plaidman.SaltShuffleRevival {
	[PlayerMutator]
	class FactionTrackerInit : IPlayerMutator {
		public void mutate(GameObject go) {
			FactionTracker.GetInstance();
		}
	}

	[Serializable]
	class FactionTracker : IPlayerSystem {
		[NonSerialized]
		private static FactionTracker Instance;
		[NonSerialized]
		const string UninstallCommand = "Plaidman_SaltShuffleRevival_Command_Uninstall";
		public Dictionary<string, List<FactionEntity>> FactionMemberCache;

		public override bool WantFieldReflection => false;
		public override void Write(SerializationWriter writer) { writer.WriteNamedFields(this, GetType()); }
		public override void Read(SerializationReader reader) { reader.ReadNamedFields(this, GetType()); }

		public static FactionTracker GetInstance() {
			if (Instance != null) return Instance;

			Instance = The.Game?.RequireSystem<FactionTracker>();
			return Instance ?? new();
		}

		public FactionTracker() {
			FactionMemberCache = new();

			var factionList = Factions.GetList().Where(f => {
				return f.Visible && GameObjectFactory.Factory.AnyFactionMembers(f.Name);
			});

			foreach (var faction in factionList) {
				var factionMembers = GameObjectFactory.Factory.GetFactionMembers(faction.Name)
					.Select(bp => new FactionEntity(bp.Name))
					.ToList();
				FactionMemberCache.Add(faction.Name, factionMembers);
			}
		}

		public override void Register(XRLGame game, IEventRegistrar registrar) {
			registrar.Register(AfterZoneBuiltEvent.ID);
			base.Register(game, registrar);
		}

		public override void RegisterPlayer(GameObject player, IEventRegistrar registrar) {
			registrar.Register(CommandEvent.ID);
			base.RegisterPlayer(player, registrar);
		}

		public override bool HandleEvent(AfterZoneBuiltEvent e) {
			var creatures = e.Zone.GetObjects(go => GetCreatureFactions(go).Count > 0);
			foreach (var creature in creatures) {
				AddFactionMember(creature);
			}

			return base.HandleEvent(e);
		}

		public override bool HandleEvent(CommandEvent e) {
			if (e.Command == UninstallCommand) {
				UninstallParts();
			}

			return base.HandleEvent(e);
		}

		public void UninstallParts() {
			if (!Confirm.ShowNoYes("Are you sure you want to uninstall {{W|Salt Shuffle Revival}}? All cards and booster packs will be removed.")) {
				XRL.Messages.MessageQueue.AddPlayerMessage("{{W|Salt Shuffle Revival}} uninstall was cancelled.");
				return;
			}

			The.Game.HandleEvent(new SSR_UninstallEvent());
			The.Game.RemoveSystem(this);

			Popup.Show("Finished removing {{W|Salt Shuffle Revival}}. Please save and quit, then you can remove this mod.");
		}

		public static bool FactionHasMembers(string faction) {
			var instance = GetInstance();

			if (instance.FactionMemberCache.TryGetValue(faction, out List<FactionEntity> factionMembers)) {
				return factionMembers.Count > 0;
			}
			
			return false;
		}

		private static List<FactionEntity> GetFactionMembers(string faction) {
			var instance = GetInstance();

			if (instance.FactionMemberCache.TryGetValue(faction, out List<FactionEntity> factionMembers)) {
				return factionMembers;
			}

			factionMembers = new();
			instance.FactionMemberCache.Add(faction, factionMembers);
			return factionMembers;
		}

		private static void AddFactionMember(GameObject go) {
			if (go.GetBlueprint().IsBaseBlueprint()) {
				return;
			}

			var entity = new FactionEntity(go, false);
			foreach (var faction in entity.Factions) {
				var factionMembers = GetFactionMembers(faction);
				if (factionMembers.Any(member => member.Equals(entity))) continue;
				factionMembers.Add(entity);
			}
		}

		private static IEnumerable<string> GetNonEmptyFactions() {
			return GetInstance().FactionMemberCache
				.Where(kvp => kvp.Value.Count > 0)
				.Select(kvp => kvp.Key);
		}

		public static string ClosestFaction(string faction) {
			var keys = GetNonEmptyFactions();
			var factionToLower = faction.ToLower();
			var closest = "";
			var min = int.MaxValue;

			foreach (var key in keys) {
				var keyToLower = key.ToLower();
				if (keyToLower.StartsWith("villagers of ")) {
					keyToLower = keyToLower[13..];
				}

				var dist = Grammar.LevenshteinDistance(factionToLower, keyToLower, false);
				if (dist >= min) continue;

				closest = key;
				min = dist;
			}

			return closest;
		}

		public static string GetRandomFaction() {
			return GetNonEmptyFactions().GetRandomElementCosmetic();
		}

		public static FactionEntity GetRandomCreature(string faction = null) {
			faction ??= GetRandomFaction();
			return GetFactionMembers(faction).GetRandomElementCosmetic().GetCreature();
		}

		public static List<string> GetCreatureFactions(GameObject go) {
			if (go.Brain == null) return new();

			return go.Brain.Allegiance
				.Where(kvp => {
					if (Brain.GetAllegianceLevel(kvp.Value) != Brain.AllegianceLevel.Member) return false;
					return Factions.Get(kvp.Key).Visible;
				})
				.Select(kvp => kvp.Key)
				.ToList();
		}
	}
}