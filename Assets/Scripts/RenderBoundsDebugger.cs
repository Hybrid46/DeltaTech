using UnityEngine;

public class RenderBoundsDebugger : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        GizmoExtension.GizmosExtend.DrawBounds(GetComponent<MeshRenderer>().bounds, Color.yellow);
    }
}
