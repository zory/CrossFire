using UnityEditor;
using UnityEngine;

namespace CrossFire.Physics
{
	[CustomEditor(typeof(ColliderAuthoring))]
	public class ColliderAuthoringEditor : Editor
	{
		private SerializedProperty _colliderType;
		private SerializedProperty _outlineVertices;
		private SerializedProperty _colliderBoundRadius;
		private SerializedProperty _colliderCircleRadius;

		private void OnEnable()
		{
			_colliderType = serializedObject.FindProperty("ColliderType");
			_outlineVertices = serializedObject.FindProperty("OutlineVertices");
			_colliderBoundRadius = serializedObject.FindProperty("ColliderBoundRadius");
			_colliderCircleRadius = serializedObject.FindProperty("ColliderCircleRadius");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			ColliderAuthoring authoring = (ColliderAuthoring)target;
			Collider2DType colliderType = (Collider2DType)_colliderType.enumValueIndex;

			EditorGUILayout.PropertyField(_colliderType);

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Collider", EditorStyles.boldLabel);

			switch (colliderType)
			{
				case Collider2DType.Circle:
					EditorGUILayout.PropertyField(_colliderCircleRadius);

					float circleRadius = Mathf.Max(0f, _colliderCircleRadius.floatValue);
					_colliderCircleRadius.floatValue = circleRadius;
					_colliderBoundRadius.floatValue = circleRadius;

					EditorGUI.BeginDisabledGroup(true);
					EditorGUILayout.FloatField("Collider Bound Radius", circleRadius);
					EditorGUI.EndDisabledGroup();
					break;

				case Collider2DType.ConcaveTriangles:
					EditorGUILayout.PropertyField(_outlineVertices, true);

					float calculatedBoundRadius = authoring.CalculateBoundRadius();
					_colliderBoundRadius.floatValue = Mathf.Max(0f, calculatedBoundRadius);

					EditorGUI.BeginDisabledGroup(true);
					EditorGUILayout.FloatField("Collider Bound Radius", _colliderBoundRadius.floatValue);
					EditorGUI.EndDisabledGroup();
					break;
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}