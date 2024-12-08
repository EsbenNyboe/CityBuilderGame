using Unity.Entities;
using UnityEngine;

namespace Rendering
{
    public struct SortingJobConfig : IComponentData
    {
        public int SectionsPerSplitJob;
        public int SplitJobCount;
        public bool EnableGizmos;
        public bool EnableDebugLog;
    }

    public class SortingJobConfigAuthoring : MonoBehaviour
    {
        public int SectionsPerSplitJob;
        public int SplitJobCount;
        public bool EnableGizmos;
        public bool EnableDebugLog;

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
                        EnableDebugLog = authoring.EnableDebugLog
                    });
            }
        }
    }
}