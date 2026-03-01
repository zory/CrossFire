using CrossFire;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[BurstCompile]
public partial struct CameraFollowSystem : ISystem
{
	public void OnCreate(ref SystemState state)
	{
		state.RequireForUpdate<ControlledTag>();
	}

	public void OnUpdate(ref SystemState state)
	{
		if (CameraReference.Instance == null) return;

		var camera = CameraReference.Instance.Camera;
		if (camera == null) return;

		float2 targetPosition = float2.zero;
		bool found = false;

		foreach (var transform in
				 SystemAPI.Query<RefRO<WorldPose>>()
				 .WithAll<ControlledTag>())
		{
			targetPosition = transform.ValueRO.Value.Position;
			found = true;
			break; // assume single controlled entity
		}

		if (!found) return;

		Vector3 current = camera.transform.position;
		Vector3 target = new Vector3(targetPosition.x, targetPosition.y, current.z);
		camera.transform.position = Vector3.Lerp(current, target, 10f * SystemAPI.Time.DeltaTime);
	}
}