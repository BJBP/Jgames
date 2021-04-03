using UnityEngine;
using UnityEngine.UI;

namespace BuildHelper.Examples {
	[RequireComponent(typeof(Text))]
	public class VersionView : MonoBehaviour {
		private void Start () {
			var txt = GetComponent<Text>();
			txt.text = Application.version;
		}
	}
}
