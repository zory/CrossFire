using UnityEditor;
using UnityEngine;

namespace CrossFire.HexMap
{
    public class GlowingHexOutlineUI : MonoBehaviour
    {
		public GameObject[] EdgesGameObjects;
		public float Radius = 1f;
		public float Percentage = 1;

		[ContextMenu("Rebuild Selection Marker")]
		public void RebuildSelectionMarker()
		{
			if (EdgesGameObjects == null || EdgesGameObjects.Length != 6)
			{
				Debug.LogError("EdgesGameObjects must contain exactly 6 objects.", this);
				return;
			}

			for (int i = 0; i < 6; i++)
			{
				if (EdgesGameObjects[i] == null)
					continue;

				Transform edge = EdgesGameObjects[i].transform;

				Undo.RecordObject(edge, "Rebuild Marker");

				edge.localPosition = HexHelpers.GetPointyHexEdgeMidpointXZ(i, Radius);
				edge.localRotation = Quaternion.Euler(0, HexHelpers.GetPointyHexEdgeRotationDeg(i), 0);
				edge.localScale = new Vector3(HexHelpers.GetPointyHexApothem(Radius) * Percentage, edge.localScale.y, edge.localScale.z);
				EditorUtility.SetDirty(edge.gameObject);
			}
		}
	}
}
