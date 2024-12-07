using Unity.Entities;
using UnityEngine;

namespace Rendering
{
    public struct SortingTest : IComponentData
    {
        public int SectionsPerSplitJob;
        public int SplitJobCount;
    }

    public class SortingTestAuthoring : MonoBehaviour
    {
        public int SectionsPerSplitJob;
        public int SplitJobCount;

        public class SortingTestBaker : Baker<SortingTestAuthoring>
        {
            public override void Bake(SortingTestAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity,
                    new SortingTest { SectionsPerSplitJob = authoring.SectionsPerSplitJob, SplitJobCount = authoring.SplitJobCount });
            }
        }
    }
}