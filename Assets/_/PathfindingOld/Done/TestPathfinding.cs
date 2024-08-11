using UnityEngine;
using CodeMonkey;
using CodeMonkey.Utils;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
/*
public class TestPathfinding : ComponentSystem {

    protected override void OnUpdate() {
        if (Input.GetMouseButtonDown(0)) {
            Vector3 mousePosition = UtilsClass.GetMouseWorldPosition();

            float cellSize = PathfindingGridSetup.Instance.pathfindingGrid.GetCellSize();

            PathfindingGridSetup.Instance.pathfindingGrid.GetXY(mousePosition + new Vector3(1, 1) * cellSize * +.5f, out int endX, out int endY);
            
            ValidateGridPosition(ref endX, ref endY);
            //CMDebug.TextPopupMouse(x + ", " + y);

            Entities.ForEach((Entity entity, DynamicBuffer<PathPosition> pathPositionBuffer, ref Translation translation) => {
                //Debug.Log("Add Component!");
                PathfindingGridSetup.Instance.pathfindingGrid.GetXY(translation.Value + new float3(1, 1, 0) * cellSize * +.5f, out int startX, out int startY);

                ValidateGridPosition(ref startX, ref startY);

                EntityManager.AddComponentData(entity, new PathfindingParams { 
                    startPosition = new int2(startX, startY), endPosition = new int2(endX, endY) 
                });
            });
        }
    }

    private void ValidateGridPosition(ref int x, ref int y) {
        x = math.clamp(x, 0, PathfindingGridSetup.Instance.pathfindingGrid.GetWidth() - 1);
        y = math.clamp(y, 0, PathfindingGridSetup.Instance.pathfindingGrid.GetHeight() - 1);
    }

}
*/