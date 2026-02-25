//using Unity.Entities;
//using Unity.Transforms;
//using UnityEngine;

//public class EcsCameraFollow : MonoBehaviour
//{
//	public float smooth = 10f;

//	EntityManager em;

//	void Awake()
//	{
//		var world = World.DefaultGameObjectInjectionWorld;
//		if (world != null)
//			em = world.EntityManager;
//	}

//	void LateUpdate()
//	{
//		if (em == null) return;

//		// Find ControlledShip singleton without keeping an EntityQuery alive
//		Entity controlledSingleton = Entity.Null;

//		using (var q = em.CreateEntityQuery(typeof(ControlledShip)))
//		{
//			if (q.CalculateEntityCount() == 0) return;
//			controlledSingleton = q.GetSingletonEntity();
//		}

//		var controlled = em.GetComponentData<ControlledShip>(controlledSingleton).Value;
//		if (controlled == Entity.Null || !em.Exists(controlled)) return;
//		if (!em.HasComponent<LocalTransform>(controlled)) return;

//		var lt = em.GetComponentData<LocalTransform>(controlled);
//		var desired = new Vector3(lt.Position.x, lt.Position.y, transform.position.z);

//		transform.position = Vector3.Lerp(
//			transform.position,
//			desired,
//			1f - Mathf.Exp(-smooth * Time.deltaTime));
//	}
//}