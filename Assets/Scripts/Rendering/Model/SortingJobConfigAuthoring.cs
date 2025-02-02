using Unity.Entities;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Rendering
{
    public struct SortingJobConfig : IComponentData
    {
        public int SectionsPerSplitJob;
        public int SplitJobCount;
        public bool EnableGizmos;
        public bool EnableDebugLog;
        public bool DisplayRenderInstanceCount;
        public int RenderInstanceCount;
    }

    public class SortingJobConfigAuthoring : MonoBehaviour
    {
        [Range(2, 20)] public int SectionsPerSplitJob;

        [Range(1, 4)] public int SplitJobCount;

        public bool EnableGizmos;
        public bool EnableDebugLog;
        public bool DisplayRenderInstanceCount;

        [HideInInspector] public int RenderInstanceCount;

        public class SortingTestBaker : Baker<SortingJobConfigAuthoring>
        {
            public override void Bake(SortingJobConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity,
                    new SortingJobConfig
                    {
                        SectionsPerSplitJob = authoring.SectionsPerSplitJob,
                        SplitJobCount = authoring.SplitJobCount,
                        EnableGizmos = authoring.EnableGizmos,
                        EnableDebugLog = authoring.EnableDebugLog,
                        DisplayRenderInstanceCount = authoring.DisplayRenderInstanceCount
                    });
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(SortingJobConfigAuthoring))]
        public class SortingJobConfigAuthoringEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();
                var sortingJobConfigAuthoring = (SortingJobConfigAuthoring)target;
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.IntField("Render Instance Count", sortingJobConfigAuthoring.RenderInstanceCount);
                EditorGUI.EndDisabledGroup();
            }
        }
#endif
    }
}