using CrossFire.Core;
using Core.Physics;
using CrossFire.Ships;
using CrossFire.Targeting;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace CrossFire.Samples
{
	public class GameplaySample : MonoBehaviour
	{
		[Header("SpawnShip")]
		public bool SpawnShip;
		public int SpawnShip_Id;
		public ShipType SpawnShip_Type;
		public byte SpawnShip_Team;
		public Pose2D SpawnShip_Pose;

		[Header("Selectable")]
		public bool ListenForSelectableWithMouse;
		public float PickRadius = 0.5f;

		[Header("Ship controll")]
		public bool ShipControl;

		[Header("Ship controll")]
		public bool CreateBattleGround;

		[Header("Targeting")]
		public bool Targeting;
		public int Targeting_ShipId = 1;
		public MovementTargetMode Targeting_Mode = MovementTargetMode.FlyToPoint;
		public float2 Targeting_WorldPosition;
		public int Targeting_TargetShipId;
		public float Targeting_PreferredDistance = 8f;
		public float Targeting_DistanceTolerance = 2f;
		public float Targeting_ArrivalDistance = 0.5f;

		private void Start()
		{
			if (CreateBattleGround)
			{
				CreateBattleGround = false;

				EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

				for (int teamIdx = 0; teamIdx < 2; teamIdx++)
				{
					byte team = (byte)teamIdx;

					for (int i = 0; i < 50; i++)
					{
						ShipType type;
						if (i == 0)
						{
							type = ShipType.Carrier;
						}
						else
						{
							int shipTypeInt = UnityEngine.Random.Range(0, 3);
							if (shipTypeInt == 0)
							{
								type = ShipType.Bomber;
							}
							else
							{
								type = ShipType.Fighter;
							}
						}

						Pose2D pose = new Pose2D
						{
							Position = UnityEngine.Random.insideUnitCircle * 50f,
							ThetaRad = UnityEngine.Random.Range(0f, 2 * math.PI)
						};
						SpawnShipsCommand command = new SpawnShipsCommand()
						{
							Id = SpawnShip_Id,
							Type = type,
							Team = team,
							Pose = pose
						};
						Debug.Log($"PlayerDebug: SpawnShip Command: {command}");

						EntityQuery query = entityManager.CreateEntityQuery(typeof(SpawnShipsCommandBufferTag));
						Entity entity = query.GetSingletonEntity();
						DynamicBuffer<SpawnShipsCommand> commandBuffer = entityManager.GetBuffer<SpawnShipsCommand>(entity);
						commandBuffer.Add(command);

						SpawnShip_Id++;
					}
				}
			}
		}

		public void Update()
		{
			EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

			if (SpawnShip)
			{
				SpawnShip = false;

				SpawnShipsCommand command = new SpawnShipsCommand()
				{
					Id = SpawnShip_Id,
					Type = SpawnShip_Type,
					Team = SpawnShip_Team,
					Pose = SpawnShip_Pose
				};
				Debug.Log($"PlayerDebug: SpawnShip Command: {command}");

				EntityQuery query = entityManager.CreateEntityQuery(typeof(SpawnShipsCommandBufferTag));
				Entity entity = query.GetSingletonEntity();
				DynamicBuffer<SpawnShipsCommand> commandBuffer = entityManager.GetBuffer<SpawnShipsCommand>(entity);
				commandBuffer.Add(command);

				SpawnShip_Id++;
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
				//Debug.Log($"PlayerDebug: Movement Command: {command}");

				// Just for testing, set the PlayerInput singleton to some constant values
				EntityQuery query = entityManager.CreateEntityQuery(typeof(ShipControlIntentCommandBufferTag));
				Entity entity = query.GetSingletonEntity();
				DynamicBuffer<ShipControlIntentCommand> commandBuffer = entityManager.GetBuffer<ShipControlIntentCommand>(entity);
				commandBuffer.Add(command);
			}

			if (Targeting)
			{
				Targeting = false;

				Entity ship = FindShip(entityManager, Targeting_ShipId);
				if (ship == Entity.Null)
				{
					Debug.Log("Ship not found");
					return;
				}

				TargetReference targetRef = TargetReference.None();

				if (Targeting_Mode == MovementTargetMode.FlyToPoint)
				{
					targetRef = TargetReference.FromWorldPosition(Targeting_WorldPosition);
				}
				else
				{
					Entity targetShip = FindShip(entityManager, Targeting_TargetShipId);
					if (targetShip == Entity.Null)
					{
						Debug.Log("Target ship not found");
						return;
					}

					targetRef = TargetReference.FromEntity(targetShip);
				}

				entityManager.SetComponentData(ship, new MovementTarget
				{
					Reference = targetRef,
					Mode = Targeting_Mode,
					PreferredDistance = Targeting_PreferredDistance,
					DistanceTolerance = Targeting_DistanceTolerance,
					ArrivalDistance = Targeting_ArrivalDistance
				});

				Debug.Log("Movement command issued");
			}
		}

		private Entity FindShip(EntityManager em, int id)
		{
			var query = em.CreateEntityQuery(typeof(StableId));

			using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
			using var ids = query.ToComponentDataArray<StableId>(Unity.Collections.Allocator.Temp);

			for (int i = 0; i < entities.Length; i++)
			{
				if (ids[i].Value == id)
					return entities[i];
			}

			return Entity.Null;
		}
	}
}