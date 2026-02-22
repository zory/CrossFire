using Unity.Entities;

public struct SelectedTag : IComponentData { }

public struct SelectedEntity : IComponentData
{
	public Entity Value;
}