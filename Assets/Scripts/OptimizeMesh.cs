using UnityEngine;

[ExecuteInEditMode]
public class OptimizeMesh : MonoBehaviour
{
    private void Awake()
    {
        var meshCollider = gameObject.GetComponent<MeshFilter>();
        meshCollider.sharedMesh.Simplify();
    }
}
