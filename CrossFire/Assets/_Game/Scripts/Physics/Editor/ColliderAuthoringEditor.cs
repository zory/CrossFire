using CrossFire;
using UnityEditor;
using UnityEngine;

namespace CrossFire.Physics
{
	[CustomEditor(typeof(ColliderAuthoring))]
	public class ColliderAuthoringEditor : Editor
	{
		SerializedProperty _colliderType;
		SerializedProperty _autoCalculateBoundRadius;
		SerializedProperty _colliderBoundRadius;
		SerializedProperty _colliderCircleRadius;
		SerializedProperty _outlineVertices;
		SerializedProperty _drawBoundRadius;
		SerializedProperty _drawCircleRadius;
		SerializedProperty _drawOutline;
		SerializedProperty _drawTrianglesPreview;

		void OnEnable()
		{
			_colliderType = serializedObject.FindProperty("ColliderType");
			_autoCalculateBoundRadius = serializedObject.FindProperty("AutoCalculateBoundRadius");
			_colliderBoundRadius = serializedObject.FindProperty("ColliderBoundRadius");
			_colliderCircleRadius = serializedObject.FindProperty("ColliderCircleRadius");
			_outlineVertices = serializedObject.FindProperty("OutlineVertices");
			_drawBoundRadius = serializedObject.FindProperty("DrawBoundRadius");
			_drawCircleRadius = serializedObject.FindProperty("DrawCircleRadius");
			_drawOutline = serializedObject.FindProperty("DrawOutline");
			_drawTrianglesPreview = serializedObject.FindProperty("DrawTrianglesPreview");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			var authoring = (ColliderAuthoring)target;
			var colliderType = (Collider2DType)_colliderType.enumValueIndex;

			EditorGUILayout.PropertyField(_colliderType);
			EditorGUILayout.PropertyField(_autoCalculateBoundRadius);

			if (colliderType == Collider2DType.Circle)
			{
				EditorGUILayout.PropertyField(_colliderCircleRadius);
			}

			if (colliderType == Collider2DType.ConcaveTriangles)
			{
				EditorGUILayout.PropertyField(_outlineVertices, true);
			}

			if (_autoCalculateBoundRadius.boolValue)
			{
				float calculated = authoring.CalculateBoundRadius();
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.FloatField("Collider Bound Radius", calculated);
				EditorGUI.EndDisabledGroup();

				_colliderBoundRadius.floatValue = calculated;
			}
			else
			{
				EditorGUILayout.PropertyField(_colliderBoundRadius);
				if (_colliderBoundRadius.floatValue < 0f)
					_colliderBoundRadius.floatValue = 0f;
			}

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Gizmos", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(_drawBoundRadius);

			if (colliderType == Collider2DType.Circle)
				EditorGUILayout.PropertyField(_drawCircleRadius);

			if (colliderType == Collider2DType.ConcaveTriangles)
			{
				EditorGUILayout.PropertyField(_drawOutline);
				EditorGUILayout.PropertyField(_drawTrianglesPreview);
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}