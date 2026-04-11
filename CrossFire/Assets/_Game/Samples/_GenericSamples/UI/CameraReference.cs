using UnityEngine;

public class CameraReference : MonoBehaviour
{
	public static CameraReference Instance;
	public Camera Camera;

	void Awake()
	{
		Instance = this;
	}
}