
using CrossFire.Core;
using Core.Physics;
using CrossFire.Ships;
using CrossFire.Targeting;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace CrossFire.Samples
{
	public class GameplaySample : MonoBehaviour
	{
		[Header("Spawn Single Ship")]
		public bool SpawnShip;
		public ShipType SpawnShip_Type;
		public byte SpawnShip_Team;
		public Pose2D SpawnShip_Pose;

		[Header("Selectable")]
		public bool ListenForSelectableWithMouse;
		public float PickRadius = 0.5f;

		[Header("Ship Control")]
		public bool ShipControl;

		[Header("Battle")]
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
							type = (shipTypeInt == 0) ? ShipType.Bomber : ShipType.Fighter;
						}

						Pose2D pose = new Pose2D
						{
							Position = UnityEngine.Random.insideUnitCircle * 50f,
							ThetaRad = UnityEngine.Random.Range(0f, 2 * math.PI)
						};

						ShipSpawner.Spawn(type, team, pose);
					}
				}
			}
		}

		public void Update()
		{
			if (SpawnShip)
			{
				SpawnShip = false;
				ShipSpawner.Spawn(SpawnShip_Type, SpawnShip_Team, SpawnShip_Pose);
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

					EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
					EntityQuery query = entityManager.CreateEntityQuery(typeof(SelectionRequestBufferTag));
					Entity entity = query.GetSingletonEntity();
					DynamicBuffer<SelectionRequestCommand> commandBuffer = entityManager.GetBuffer<SelectionRequestCommand>(entity);
					commandBuffer.Add(command);
					query.Dispose();
				}
			}

			if (ShipControl)
			{
				float turn = 0f;
				if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) { turn += 1f; }
				if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) { turn -= 1f; }

				float thrust = 0f;
				if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) { thrust += 1f; }
				if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) { thrust -= 1f; }

				bool fire = Input.GetKey(KeyCode.Space);

				ShipControlIntentCommand command = new ShipControlIntentCommand
				{
					Turn = turn,
					Thrust = thrust,
					Fire = fire,
				};

				EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
				EntityQuery query = entityManager.CreateEntityQuery(typeof(ShipControlIntentCommandBufferTag));
				Entity entity = query.GetSingletonEntity();
				DynamicBuffer<ShipControlIntentCommand> commandBuffer = entityManager.GetBuffer<ShipControlIntentCommand>(entity);
				commandBuffer.Add(command);
				query.Dispose();
			}

			if (Targeting)
			{
				Targeting = false;

				EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

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

		private Entity FindShip(EntityManager entityManager, int id)
		{
			EntityQuery query = entityManager.CreateEntityQuery(typeof(StableId));

			using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
			using NativeArray<StableId> ids = query.ToComponentDataArray<StableId>(Allocator.Temp);

			for (int i = 0; i < entities.Length; i++)
			{
				if (ids[i].Value == id)
				{
					return entities[i];
				}
			}

			query.Dispose();
			return Entity.Null;
		}
	}
}
