using UnityEngine;
using UnityEngine.VFX;

public class SimpleVFXSpawner : MonoBehaviour
{
	public VisualEffect Vfx;

	void Update()
	{
		if (Input.GetMouseButtonDown(0))
		{
			Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			pos.z = 0f;

			Vfx.SetVector3("SpawnPosition", pos);
			Vfx.SendEvent("OnPlay");
		}
	}
}