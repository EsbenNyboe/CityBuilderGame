using Unity.Entities;
using UnityEngine;

public struct FakeUnitTag1 : IComponentData, IEnableableComponent
{
}

public class FakeUnitAuthoring : MonoBehaviour
{
    [SerializeField] private bool _addExtraTags;

    public class Baker : Baker<FakeUnitAuthoring>
    {
        public override void Bake(FakeUnitAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<FakeUnitTag1>(entity);

            if (authoring._addExtraTags)
            {
                AddComponent<FakeUnitTag2>(entity);
                AddComponent<FakeUnitTag3>(entity);
                AddComponent<FakeUnitTag4>(entity);
                AddComponent<FakeUnitTag5>(entity);
            }
        }
    }
}

public struct FakeUnitTag2 : IComponentData, IEnableableComponent
{
}

public struct FakeUnitTag3 : IComponentData, IEnableableComponent
{
}

public struct FakeUnitTag4 : IComponentData, IEnableableComponent
{
}

public struct FakeUnitTag5 : IComponentData, IEnableableComponent
{
}