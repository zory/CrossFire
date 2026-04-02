using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CrossFire.HexMap
{
    public class GlowingHexOutlineUI : MonoBehaviour
    {
		public GameObject[] EdgesGameObjects;
		public float Radius = 1f;
		public float Percentage = 1f;

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
				{
					continue;
				}

				Transform edge = EdgesGameObjects[i].transform;

#if UNITY_EDITOR
				Undo.RecordObject(edge, "Rebuild Marker");
#endif

				edge.localPosition = HexHelpers.GetPointyHexEdgeMidpointXZ(i, Radius);
				edge.localRotation = Quaternion.Euler(0, HexHelpers.GetPointyHexEdgeRotationDeg(i), 0);
				edge.localScale    = new Vector3(HexHelpers.GetPointyHexApothem(Radius) * Percentage, edge.localScale.y, edge.localScale.z);

#if UNITY_EDITOR
				EditorUtility.SetDirty(edge.gameObject);
#endif
			}
		}
	}
}
