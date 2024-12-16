using SystemGroups;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace UnitBehaviours.ActionGateNS
{
    public struct RandomContainer : IComponentData, IEnableableComponent
    {
        public Random Random;
    }

    public struct ActionGate : IComponentData
    {
        public float MinTimeOfAction;
    }

    [UpdateInGroup(typeof(LifetimeSystemGroup))]
    public partial struct RandomContainerSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (randomContainer, entity) in SystemAPI.Query<RefRW<RandomContainer>>().WithDisabled<RandomContainer>().WithEntityAccess())
            {
                randomContainer.ValueRW.Random = new Random((uint)entity.Index + 1);
                SystemAPI.SetComponentEnabled<RandomContainer>(entity, true);
            }

            foreach (var randomContainer in SystemAPI.Query<RefRW<RandomContainer>>())
            {
                randomContainer.ValueRW.Random.NextBool();
            }
        }
    }
}