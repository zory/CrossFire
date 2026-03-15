using Unity.Entities;
using Unity.Mathematics;

namespace CrossFire.Core
{
	public struct TargetReference
	{
		public TargetReferenceKind Kind;
		public Entity Entity;
		public float2 WorldPosition;

		public static TargetReference None() 
		{
			return new TargetReference
			{
				Kind = TargetReferenceKind.None,
				Entity = Entity.Null,
				WorldPosition = float2.zero
			};
		}

		public static TargetReference FromEntity(Entity entity)
		{
			return new TargetReference
			{
				Kind = TargetReferenceKind.Entity,
				Entity = entity,
				WorldPosition = float2.zero
			};
		}

		public static TargetReference FromWorldPosition(float2 worldPosition)
		{
			return new TargetReference
			{
				Kind = TargetReferenceKind.WorldPosition,
				Entity = Entity.Null,
				WorldPosition = worldPosition
			};
		}
	}
}
