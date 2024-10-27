using Unity.Entities;

public partial struct GridManager
{
    #region WalkableGrid

    public bool IsWalkable(int i)
    {
        return WalkableGrid[i].IsWalkable;
    }

    // Note: Remember to call SetComponent after this method
    public void SetIsWalkable(int i, bool isWalkable)
    {
        var walkableCell = WalkableGrid[i];
        walkableCell.IsWalkable = isWalkable;
        walkableCell.IsDirty = true;
        WalkableGrid[i] = walkableCell;
        WalkableGridIsDirty = true;
    }

    #endregion

    #region OccupiableGrid

    public bool IsOccupied(int gridIndex)
    {
        var occupant = OccupiableGrid[gridIndex].Occupant;
        // TODO: Find a way to check if entity exists, without using managed code (World)
        return occupant != Entity.Null; // && World.DefaultGameObjectInjectionWorld.EntityManager.Exists(occupant);
    }

    public bool EntityIsOccupant(int i, Entity entity)
    {
        return OccupiableGrid[i].Occupant == entity;
    }

    // Note: Remember to call SetComponent after this method
    public bool TryClearOccupant(int i, Entity entity)
    {
        if (EntityIsOccupant(i, entity))
        {
            SetOccupant(i, Entity.Null);
            return true;
        }

        return false;
    }

    // Note: Remember to call SetComponent after this method
    public void SetOccupant(int i, Entity entity)
    {
        var occupiableCell = OccupiableGrid[i];
        occupiableCell.Occupant = entity;
        occupiableCell.IsDirty = true;
        OccupiableGrid[i] = occupiableCell;
        OccupiableGridIsDirty = true;
    }

    #endregion

    #region DamageableGrid

    public bool IsDamageable(int i)
    {
        return DamageableGrid[i].Health > 0;
    }

    // Note: Remember to call SetComponent after this method
    public void SetHealthToMax(int i)
    {
        SetHealth(i, DamageableGrid[i].MaxHealth);
    }

    // Note: Remember to call SetComponent after this method
    public void AddDamage(int i, float damage)
    {
        SetHealth(i, DamageableGrid[i].Health - damage);
    }

    // Note: Remember to call SetComponent after this method
    public void SetHealth(int i, float health)
    {
        var damageableCell = DamageableGrid[i];
        damageableCell.Health = health;
        damageableCell.IsDirty = true;
        DamageableGrid[i] = damageableCell;
        DamageableGridIsDirty = true;
    }

    #endregion

    #region InteractableGrid

    public bool IsInteractable(int i)
    {
        // TODO: Find a way to check if entity exists, without using managed code (World)
        var interactable = InteractableGrid[i].Interactable;
        return interactable != Entity.Null; // && World.DefaultGameObjectInjectionWorld.EntityManager.Exists(interactable);
    }

    public bool IsInteractedWith(int i)
    {
        // TODO: Find a way to check if entity exists, without using managed code (World)
        var interactor = InteractableGrid[i].Interactor;
        return interactor != Entity.Null; // && World.DefaultGameObjectInjectionWorld.EntityManager.Exists(interactable);
    }

    // Note: Remember to call SetComponent after this method
    public void SetInteractable(int i, Entity entity)
    {
        var interactableCell = InteractableGrid[i];
        interactableCell.Interactable = entity;
        InteractableGrid[i] = interactableCell;
    }

    // Note: Remember to call SetComponent after this method
    public void SetInteractor(int i, Entity entity)
    {
        var interactableCell = InteractableGrid[i];
        interactableCell.Interactor = entity;
        InteractableGrid[i] = interactableCell;
    }

    public bool EntityIsInteractor(int i, Entity entity)
    {
        return InteractableGrid[i].Interactor == entity;
    }

    // Note: Remember to call SetComponent after this method
    public bool TryClearInteractor(int i, Entity entity)
    {
        if (EntityIsInteractor(i, entity))
        {
            SetInteractor(i, Entity.Null);
            return true;
        }

        return false;
    }

    #endregion
}