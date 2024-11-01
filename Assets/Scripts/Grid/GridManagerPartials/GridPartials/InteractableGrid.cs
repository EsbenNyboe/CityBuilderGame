using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct InteractableCell
{
    public Entity Interactable;
    public Entity Interactor;
}

public partial struct GridManager
{
    #region InteractableGrid Core

    public Entity GetInteractable(int i)
    {
        return InteractableGrid[i].Interactable;
    }

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

    #region InteractableGrid Variants

    public Entity GetInteractable(Vector3 position)
    {
        GridHelpers.GetXY(position, out var x, out var y);
        return GetInteractable(x, y);
    }

    public Entity GetInteractable(int2 cell)
    {
        return GetInteractable(cell.x, cell.y);
    }

    public Entity GetInteractable(int x, int y)
    {
        var i = GetIndex(x, y);
        return GetInteractable(i);
    }

    public bool IsInteractable(Vector3 position)
    {
        GridHelpers.GetXY(position, out var x, out var y);
        return IsInteractable(x, y);
    }

    public bool IsInteractable(int2 cell)
    {
        return IsInteractable(cell.x, cell.y);
    }

    public bool IsInteractable(int x, int y)
    {
        var i = GetIndex(x, y);
        return IsInteractable(i);
    }

    public bool IsInteractedWith(Vector3 position)
    {
        GridHelpers.GetXY(position, out var x, out var y);
        return IsInteractedWith(x, y);
    }

    public bool IsInteractedWith(int2 cell)
    {
        return IsInteractedWith(cell.x, cell.y);
    }

    public bool IsInteractedWith(int x, int y)
    {
        var i = GetIndex(x, y);
        return IsInteractedWith(i);
    }

    public bool EntityIsInteractor(Vector3 position, Entity entity)
    {
        GridHelpers.GetXY(position, out var x, out var y);
        return EntityIsInteractor(x, y, entity);
    }

    public bool EntityIsInteractor(int2 cell, Entity entity)
    {
        return EntityIsInteractor(cell.x, cell.y, entity);
    }

    public bool EntityIsInteractor(int x, int y, Entity entity)
    {
        var i = GetIndex(x, y);
        return EntityIsInteractor(i, entity);
    }

    // Note: Remember to call SetComponent after this method
    public void SetInteractable(Vector3 position, Entity entity)
    {
        GridHelpers.GetXY(position, out var x, out var y);
        SetInteractable(x, y, entity);
    }

    // Note: Remember to call SetComponent after this method
    public void SetInteractable(int2 cell, Entity entity)
    {
        SetInteractable(cell.x, cell.y, entity);
    }

    // Note: Remember to call SetComponent after this method
    public void SetInteractable(int x, int y, Entity entity)
    {
        var gridIndex = GetIndex(x, y);
        SetInteractable(gridIndex, entity);
    }

    // Note: Remember to call SetComponent after this method
    public void SetInteractor(Vector3 position, Entity entity)
    {
        GridHelpers.GetXY(position, out var x, out var y);
        SetInteractor(x, y, entity);
    }

    // Note: Remember to call SetComponent after this method
    public void SetInteractor(int2 cell, Entity entity)
    {
        SetInteractor(cell.x, cell.y, entity);
    }

    // Note: Remember to call SetComponent after this method
    public void SetInteractor(int x, int y, Entity entity)
    {
        var gridIndex = GetIndex(x, y);
        SetInteractor(gridIndex, entity);
    }

    // Note: Remember to call SetComponent after this method
    public bool TryClearInteractor(Vector3 position, Entity entity)
    {
        GridHelpers.GetXY(position, out var x, out var y);
        return TryClearInteractor(x, y, entity);
    }

    // Note: Remember to call SetComponent after this method
    public bool TryClearInteractor(int2 cell, Entity entity)
    {
        return TryClearInteractor(cell.x, cell.y, entity);
    }

    // Note: Remember to call SetComponent after this method
    public bool TryClearInteractor(int x, int y, Entity entity)
    {
        var i = GetIndex(x, y);
        return TryClearInteractor(i, entity);
    }

    #endregion
}