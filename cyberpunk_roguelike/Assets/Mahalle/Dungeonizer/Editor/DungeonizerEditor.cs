using UnityEditor;
using UnityEngine;
using Dungeonizer;

namespace DungeonizerEditor {

	[CustomPropertyDrawer(typeof(OddIntRangeAttribute))]
	public class OddIntRangeDrawer : PropertyDrawer {
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
			var attr = (OddIntRangeAttribute)attribute;
			if (property.propertyType != SerializedPropertyType.Integer) {
				EditorGUI.LabelField(position, label.text, "Use OddIntRange with int.");
				return;
			}
			int newValue = EditorGUI.IntSlider(position, label, property.intValue, attr.min, attr.max);
			// Snap to odd. If max is even and we hit it via snap, step back one.
			if (newValue % 2 == 0) newValue += 1;
			if (newValue > attr.max) newValue = attr.max % 2 == 0 ? attr.max - 1 : attr.max;
			if (newValue < attr.min) newValue = attr.min % 2 == 0 ? attr.min + 1 : attr.min;
			property.intValue = newValue;
		}
	}

[CustomEditor(typeof(Dungeonizer.Dungeonizer))]
	public class DungeonizerEditor : Editor {
		
		public override void OnInspectorGUI () {
			//Called whenever the inspector is drawn for this object.,
			Dungeonizer.Dungeonizer realscript = (Dungeonizer.Dungeonizer)target;
			if(realscript.minRoomSize > realscript.maxRoomSize){
				EditorGUILayout.HelpBox("Please make sure your minumum room size isn't bigger than maximum room size.", MessageType.Error);
			}
			EditorGUILayout.HelpBox("Need open worlds too? Check out our Terrainizer asset.", MessageType.Info);
			

			DrawDefaultInspector();
			//This draws the default screen.  You don't need this if you want
			//to start from scratch, but I use this when I'm just adding a button or
			//some small addition and don't feel like recreating the whole inspector.
			
			
	
			if(GUILayout.Button("Create Now")) {
				//add everthing the button would do.
				realscript.ClearOldDungeon(true);
				realscript.Generate();
				
			}
		}
	}
}