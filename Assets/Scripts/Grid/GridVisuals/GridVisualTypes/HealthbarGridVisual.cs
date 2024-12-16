using UnityEngine;

namespace Grid.GridVisuals
{
    public class HealthbarGridVisual : GridVisual
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

            damageableCell.IsDirty = false;
            gridManager.DamageableGrid[index] = damageableCell;

            var healthNormalized = damageableCell.Health / damageableCell.MaxHealth;
            var widthPercentage = 0.75f;
            var quadWidth = healthNormalized * widthPercentage;

            var quadHeight = 0.1f;

            var cellSize = 1f; // GridManager currently only supports a cellSize of one

            quadSize = damageableCell.Health > 0
                ? new Vector3(quadWidth, quadHeight) * cellSize
                : Vector3.zero;

            var green = 0.9f;
            var yellow = 0.4f;
            var red = 0.1f;
            var color = healthNormalized switch
            {
                > 0.85f => green,
                > 0.4f => yellow,
                _ => red
            };

            uv00 = new Vector2(color, 0f);
            uv11 = new Vector2(color, 1f);

            // TODO: Make positioning cleaner? Not accounting for cell-size right now...
            // position.x += widthPercentage * 0.5f;
            worldPosition.y += 0.4f;
            if (healthNormalized < 1)
            {
                worldPosition.x -= (1 - healthNormalized) / 2;
            }
            else
            {
                // Hide health, if full
                quadSize = Vector3.zero;
            }

            return true;
        }
    }
}