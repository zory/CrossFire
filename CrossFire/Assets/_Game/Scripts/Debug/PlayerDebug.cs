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

		[Header("Ship controll")]
		public bool ShipControl;

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
				Entity entity = query.GetSingletonEntity();
				DynamicBuffer<SpawnShipsCommand> commandBuffer = entityManager.GetBuffer<SpawnShipsCommand>(entity);
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
					Entity entity = query.GetSingletonEntity();
					DynamicBuffer<SelectionRequestCommand> commandBuffer = entityManager.GetBuffer<SelectionRequestCommand>(entity);
					commandBuffer.Add(command);
				}
			}

			if (ShipControl)
			{
				float turn = 0f;
				if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) turn += 1f;
				if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) turn -= 1f;

				float thrust = 0f;
				if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) thrust += 1f;
				if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) thrust -= 1f;

				bool fire = Input.GetKey(KeyCode.Space) ? true : false;

				ShipControlIntentCommand command = new ShipControlIntentCommand
				{
					Turn = turn,
					Thrust = thrust,
					Fire = fire,
				};
				Debug.Log($"PlayerDebug: Movement Command: {command}");

				// Just for testing, set the PlayerInput singleton to some constant values
				EntityQuery query = entityManager.CreateEntityQuery(typeof(ShipControlIntentCommandBufferTag));
				Entity entity = query.GetSingletonEntity();
				DynamicBuffer<ShipControlIntentCommand> commandBuffer = entityManager.GetBuffer<ShipControlIntentCommand>(entity);
				commandBuffer.Add(command);
			}
		}
	}
}