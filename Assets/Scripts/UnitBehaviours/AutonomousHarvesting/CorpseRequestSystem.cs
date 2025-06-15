using Grid;
using Inventory;
using UnitBehaviours.AutonomousHarvesting.Model;
using Unity.Collections;
using Unity.Entities;

namespace CorpseNS
{
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    public partial struct CorpseRequestSystem : ISystem
    {
        private EntityQuery _corpseRequestQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridManager>();

            _corpseRequestQuery = state.GetEntityQuery(
                new EntityQueryDesc { All = new ComponentType[] { typeof(CorpseRequest) } }
            );
            state.RequireForUpdate(_corpseRequestQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            state.Dependency.Complete();
            var gridManager = SystemAPI.GetSingleton<GridManager>();
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var requestedCorpseEntities = new NativeList<Entity>(Allocator.Temp);

            foreach (var corpseRequest in SystemAPI.Query<RefRO<CorpseRequest>>())
            {
                var requesterEntity = corpseRequest.ValueRO.RequesterEntity;
                var corpseEntity = corpseRequest.ValueRO.CorpseEntity;

                if (!SystemAPI.Exists(requesterEntity)
                    || !SystemAPI.Exists(corpseEntity)
                    || requestedCorpseEntities.Contains(corpseEntity))
                {
                    continue;
                }

                var inventory = SystemAPI.GetComponentRW<InventoryState>(requesterEntity);
                inventory.ValueRW.CurrentItem = InventoryItem.RawMeat;
                requestedCorpseEntities.Add(corpseEntity);

//                var corpse = SystemAPI.GetComponent<Corpse>(corpseEntity);
                //               if (corpse.MeatCurrent <= 0)
                //             {
                //               ecb.DestroyEntity(corpseEntity);
                //         }
            }

            ecb.Playback(state.EntityManager);
            state.EntityManager.DestroyEntity(_corpseRequestQuery);
            SystemAPI.SetSingleton(gridManager);
        }
    }
}