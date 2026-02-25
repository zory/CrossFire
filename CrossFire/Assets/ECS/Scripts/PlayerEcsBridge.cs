//using Unity.Entities;
//using Unity.Mathematics;
//using UnityEngine;

//public class PlayerEcsBridge : MonoBehaviour
//{
//	EntityManager em;
//	Entity inputEntity;

//	void Awake()
//	{
//		em = World.DefaultGameObjectInjectionWorld.EntityManager;

//		// Create/find PlayerInput singleton
//		var q = em.CreateEntityQuery(typeof(PlayerInput));
//		if (q.CalculateEntityCount() == 0)
//		{
//			inputEntity = em.CreateEntity(typeof(PlayerInput));
//			em.SetComponentData(inputEntity, new PlayerInput());
//		}
//		else
//		{
//			inputEntity = q.GetSingletonEntity();
//		}
//		q.Dispose();
//	}

//	void Update()
//	{
//		if (!em.Exists(inputEntity)) return;

//		float turn = 0f;
//		if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) turn += 1f;
//		if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) turn -= 1f;

//		float thrust = 0f;
//		if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) thrust += 1f;
//		if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) thrust -= 1f;

//		byte fire = (byte)(Input.GetMouseButton(0) ? 1 : 0);

//		em.SetComponentData(inputEntity, new PlayerInput
//		{
//			Turn = turn,
//			Thrust = thrust,
//			Fire = fire
//		});
//	}
//}