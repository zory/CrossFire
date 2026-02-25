using CrossFire.Ships;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace CrossFire.DevelopmentTools
{
	public class PlayerDebug : MonoBehaviour
	{
		[Header("SpawnShip")]
		public bool SpawnShip;
		public int SpawnShip_Id;
		public byte SpawnShip_Team;
		public Color SpawnShip_Color;
		public Pose2D SpawnShip_Pose;

		[Header("Selectable")]
		public bool ListenForSelectableWithMouse;
		public float PickRadius = 0.5f;

		public void Update()
		{
			EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

			if (SpawnShip)
			{
				SpawnShip = false;

				SpawnShipsCommand command = new SpawnShipsCommand()
				{
					Id = SpawnShip_Id,
					Team = SpawnShip_Team,
					ColorRGBA = new float4(SpawnShip_Color.r, SpawnShip_Color.g, SpawnShip_Color.b, SpawnShip_Color.a),
					Pose = SpawnShip_Pose
				};
				Debug.Log($"PlayerDebug: SpawnShip Command: {command}");

				EntityQuery query = entityManager.CreateEntityQuery(typeof(SpawnShipsCommandBufferTag));
				Entity requestEntity = query.GetSingletonEntity();
				DynamicBuffer<SpawnShipsCommand> commandBuffer = entityManager.GetBuffer<SpawnShipsCommand>(requestEntity);
				commandBuffer.Add(command);
			}

			if (ListenForSelectableWithMouse)
			{
				Camera camera = Camera.main;
				if (Input.GetMouseButtonDown(0) && camera != null)
				{
					Vector3 mouseWorld3 = camera.ScreenToWorldPoint(Input.mousePosition);
					float2 mouseWorld2 = new float2(mouseWorld3.x, mouseWorld3.y);
					SelectionRequestCommand command = new SelectionRequestCommand
					{
						WorldPosition = mouseWorld2,
						PickRadius = PickRadius
					};
					Debug.Log($"PlayerDebug: Select Selectable Command: {command}");

					EntityQuery query = entityManager.CreateEntityQuery(typeof(SelectionRequestBufferTag));
					Entity requestEntity = query.GetSingletonEntity();
					DynamicBuffer<SelectionRequestCommand> commandBuffer = entityManager.GetBuffer<SelectionRequestCommand>(requestEntity);
					commandBuffer.Add(command);
				}
			}
		}
	}
}