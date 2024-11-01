using UnityEngine;

public class TreeGridVisual : GridVisual
{
    protected override bool TryGetUpdatedCellVisual(GridManager gridManager, int index, out Vector2 uv00, out Vector2 uv11, ref Vector3 quadSize,
        ref Vector3 worldPosition)
    {
        var damageableCell = gridManager.DamageableGrid[index];
        if (!damageableCell.IsDirty)
        {
            uv00 = default;
            uv11 = default;
            quadSize = default;
            return false;
        }

        // Note: The reason we don't call ClearDirty() is, because we need to wait for healthBars to update first
        // damageableCell.IsDirty = false;
        // gridManager.DamageableGrid[index] = damageableCell;

        var health = damageableCell.Health;

        var maxHealth = damageableCell.MaxHealth;
        var healthNormalized = health / maxHealth;

        var frameSize = 0.25f;

        var frameOffset = 0.0f; // max health tree

        if (healthNormalized < 1f)
        {
            frameOffset = 0.25f;
        }

        if (healthNormalized < 0.66f)
        {
            frameOffset = 0.5f;
        }

        if (healthNormalized < 0.33f)
        {
            frameOffset = 0.75f;
        }

        if (health <= 0)
        {
            // There's no tree
            frameOffset = 1f;
        }

        uv00 = new Vector2(frameOffset, 0);
        uv11 = new Vector2(frameOffset + frameSize, 1);

        return true;
    }
}