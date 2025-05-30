using GridEntityNS;
using Unity.Entities;
using UnityEngine;

namespace UnitBehaviours.AutonomousHarvesting
{
    [DisableAutoCreation]
    public partial struct AutoConstructionSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (constructable, autoConstruction) in SystemAPI.Query<RefRW<Constructable>, RefRW<AutoConstruction>>())
            {
                autoConstruction.ValueRW.Timer += Time.deltaTime;
                if (autoConstruction.ValueRO.Timer > 1)
                {
                    autoConstruction.ValueRW.Timer = 0;
                    constructable.ValueRW.Materials++;
                    if (constructable.ValueRO.Materials > constructable.ValueRO.MaterialsRequired)
                    {
                        constructable.ValueRW.Materials = constructable.ValueRO.MaterialsRequired;
                    }
                }
            }
        }
    }
}