using XRL.World;

namespace Plaidman.AnEyeForValue.Utils {
	public class LabelUtils {
		public static string GetValueLabel(GameObject go, bool known) {
			var ratio = ValueUtils.GetValueRatio(go);
			
			if (ratio == null) {
				// not sellable: grey
				return "{{K||X|}}";
			}

			if (double.IsPositiveInfinity((double)ratio)) {
				// zero weight object: blue
				return "{{b||0#|}}";
			}

			if (!known) {
				// not known: beige, display weight
				return GetWeightLabel(go);
			}
			
			if (ratio < 1) {
				// super low ratio: red
				return "{{R||$|}}";
			}

			if (ratio < 4) {
				// less than water: yellow
				return "{{W||$|}}";
			}

			if (ratio < 10) {
				// less than copper nugget: 1x green
				return "{{G||$|}}";
			}

			if (ratio <= 50) {
				// less than silver nugget 2x green
				return "{{G||$$|}}";
			}

			// more than silver nugget: 3x green
			return "{{G||$$$|}}";
		}
		
		public static string GetWeightLabel(GameObject go) {
			var weight = go.Weight;
			
			if (weight > 999) {
				return "{{w||999+|}}";
			}

			if (weight < -99) {
				return "{{w||-99+|}}";
			}

			return "{{w||" + weight + "#|}}";
		}
	}
}