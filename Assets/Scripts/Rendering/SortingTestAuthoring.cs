using Unity.Entities;
using UnityEngine;

namespace Rendering
{
    public struct SortingJobConfig : IComponentData
    {
        public int SectionsPerSplitJob;
        public int SplitJobCount;
        public bool EnableDebugging;
    }

    public class SortingTestAuthoring : MonoBehaviour
    {
        public int SectionsPerSplitJob;
        public int SplitJobCount;
        public bool EnableDebugging;

        public class SortingTestBaker : Baker<SortingTestAuthoring>
        {
            public override void Bake(SortingTestAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity,
                    new SortingJobConfig
                    {
                        SectionsPerSplitJob = authoring.SectionsPerSplitJob,
                        SplitJobCount = authoring.SplitJobCount,
                        EnableDebugging = authoring.EnableDebugging
                    });
            }
        }
    }
}