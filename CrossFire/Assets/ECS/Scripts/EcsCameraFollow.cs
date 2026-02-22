using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class EcsCameraFollow : MonoBehaviour
{
	public float smooth = 10f;

	EntityManager em;
	EntityQuery qControlled;

	void Awake()
	{
		em = World.DefaultGameObjectInjectionWorld.EntityManager;
		qControlled = em.CreateEntityQuery(typeof(ControlledShip));
	}

	void LateUpdate()
	{
		if (qControlled.CalculateEntityCount() == 0) return;

		var controlled = qControlled.GetSingleton<ControlledShip>().Value;
		if (controlled == Entity.Null || !em.Exists(controlled)) return;
		if (!em.HasComponent<LocalTransform>(controlled)) return;

		var lt = em.GetComponentData<LocalTransform>(controlled);
		Vector3 desired = new Vector3(lt.Position.x, lt.Position.y, transform.position.z);

		transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-smooth * Time.deltaTime));
	}

	void OnDestroy()
	{
		if (qControlled != null) qControlled.Dispose();
	}
}