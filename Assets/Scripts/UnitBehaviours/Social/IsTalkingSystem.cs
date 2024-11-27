using UnitAgency;
using UnitState;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace UnitBehaviours.Talking
{
    /// <summary>
    ///     Indicates that we are talking with someone, or trying to, at least!
    /// </summary>
    public struct IsTalking : IComponentData, IEnableableComponent
    {
    }

    [UpdateInGroup(typeof(UnitBehaviourSystemGroup))]
    public partial struct IsTalkingSystem : ISystem
    {
        private const float LonelinessReductionFactor = 10f;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridManager>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var gridManager = SystemAPI.GetSingleton<GridManager>();
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var isTalkingLookup = SystemAPI.GetComponentLookup<IsTalking>();

            // Initialization:
            foreach (var (_, entity) in SystemAPI.Query<RefRO<SocialRelationships>>().WithDisabled<IsTalking>()
                         .WithEntityAccess())
            {
                if (TryFindConversationToEngageIn(ref state, entity))
                {
                    // I will start talking!
                    ecb.SetComponentEnabled<IsTalking>(entity, true);
                }
                else
                {
                    Debug.LogError("Can't find conversation to engage in");
                    ecb.RemoveComponent<IsTalking>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                }
            }

            // Post-initialization:
            foreach (var (moodLoneliness, pathFollow, localTransform, spriteTransform, entity) in SystemAPI
                         .Query<RefRW<MoodLoneliness>, RefRO<PathFollow>, RefRO<LocalTransform>,
                             RefRW<SpriteTransform>>()
                         .WithEntityAccess().WithAll<IsTalking>())
            {
                if (pathFollow.ValueRO.IsMoving())
                {
                    // TODO: Consider removing this
                    Debug.LogError("Why are we moving?");
                    continue;
                }

                if (moodLoneliness.ValueRO.Loneliness <= 0)
                {
                    // I don't feel like talking anymore!
                    ecb.RemoveComponent<IsTalking>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                }

                var position = localTransform.ValueRO.Position;
                var cell = GridHelpers.GetXY(position);
                if (TalkingHelpers.TryGetNeighbourWithComponent(gridManager, cell, isTalkingLookup,
                        out var neighbourCell))
                {
                    moodLoneliness.ValueRW.Loneliness -= LonelinessReductionFactor * SystemAPI.Time.DeltaTime;

                    var talkingDirection = neighbourCell.x - cell.x;
                    var angleInDegrees = talkingDirection > 0 ? 0f : 180f;
                    spriteTransform.ValueRW.Rotation = quaternion.EulerZXY(0, math.PI / 180 * angleInDegrees, 0);
                }
                else
                {
                    // I have no one to talk to!
                    ecb.RemoveComponent<IsTalking>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                }
            }
        }

        private bool TryFindConversationToEngageIn(ref SystemState state, Entity entity)
        {
            foreach (var conversationEvent in SystemAPI.Query<RefRO<ConversationEvent>>())
            {
                if (entity == conversationEvent.ValueRO.Initiator ||
                    entity == conversationEvent.ValueRO.Target)
                {
                    return true;
                }
            }

            return false;
        }
    }
}