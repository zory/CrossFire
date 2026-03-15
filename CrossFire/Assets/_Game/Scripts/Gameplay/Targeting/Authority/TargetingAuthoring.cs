using CrossFire.Core;
using Unity.Entities;
using UnityEngine;

namespace CrossFire.Targeting
{
	public class TargetingAuthoring : MonoBehaviour
	{
		public bool IsTargetable = true;
		public TargetingMode TargetingMode = TargetingMode.StickyNearest;
		public float RetargetInterval = 2f;

		class Baker : Baker<TargetingAuthoring>
		{
			public override void Bake(TargetingAuthoring authoring)
			{
				Entity prefabEntity = GetEntity(TransformUsageFlags.Dynamic);

				if (authoring.IsTargetable)
				{
					AddComponent<TargetableTag>(prefabEntity);
				}

				AddComponent(prefabEntity, new CurrentTarget
				{
					Value = Entity.Null
				});

				AddComponent(prefabEntity, new ManualTarget
				{
					Value = Entity.Null
				});

				AddComponent(prefabEntity, new TargetingProfile
				{
					Mode = authoring.TargetingMode,
					RetargetInterval = authoring.RetargetInterval
				});

				AddComponent(prefabEntity, new TargetRetargetTimer
				{
					TimeLeft = authoring.RetargetInterval
				});

				if (authoring.TargetingMode != TargetingMode.Manual)
				{
					AddComponent<NeedsTargetTag>(prefabEntity);
				}
			}
		}
	}
}