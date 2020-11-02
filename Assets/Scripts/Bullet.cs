using UnityEngine;
using System.Collections.Generic;

public class Bullet : MonoBehaviour
{
    public float hitPower;
    public float hitRange;
    public float movementSpeed;
    public float spawnHeight;
    public GameObject explosion;

    private float time;
    private Vector3 direction;
    
    public void Init(RaycastHit hitRay)
    {
        transform.position = new Vector3(hitRay.point.x, spawnHeight, Camera.main.transform.position.z);
        direction = (hitRay.point - transform.position).normalized;
    }
    
    private void Update()
    {
        TryDestroyDelay();

        var previous = transform.position;
        transform.Translate(direction * Time.deltaTime * movementSpeed);
        var next = transform.position;

        if (Physics.Linecast(previous, next, out var raycastHit))
        {
            CreatExplosion(raycastHit);
            var breakVectorNorm = (raycastHit.point - previous).normalized;
            BreakCollider(raycastHit, breakVectorNorm);
            Destroy(gameObject);
        }
    }

    private void BreakCollider(RaycastHit raycastHit, Vector3 breakVectorNorm)
    {
        var pieces = new List<MeshDestroy.PartMeshData>();

        var colliders = Physics.OverlapSphere(raycastHit.point, hitRange);
        foreach (var coll in colliders)
        {
            if (!coll.transform.CompareTag("Environment") && coll.gameObject != raycastHit.transform.gameObject)
            {
                var breakVector = coll.transform.position - raycastHit.transform.position;
                var meshes = MeshDestroy.BreakGameObject(coll.gameObject, raycastHit.point, breakVector);
                foreach (var mesh in meshes)
                    pieces.Add(mesh);
                
                if (meshes.Count == 0)
                {
                    var c = MeshDestroy.CreatePartMeshData(coll.gameObject);
                    pieces.Add(c);
                }
            }
        }

        if (!raycastHit.transform.CompareTag("Environment"))
        {
            var breakVector = breakVectorNorm * hitRange;
            var meshes = MeshDestroy.BreakGameObject(raycastHit.transform.gameObject, raycastHit.point, breakVector);
            foreach (var mesh in meshes)
                pieces.Add(mesh);
            
            if (meshes.Count == 0)
            {
                var c = MeshDestroy.CreatePartMeshData(raycastHit.transform.gameObject);
                pieces.Add(c);
            }
        }

        ApplyForceToPieces(pieces, raycastHit.point);
    }

    private void TryDestroyDelay()
    {
        time += Time.deltaTime;
        if (time > 20f)
        {
            Destroy(gameObject);
        }
    }

    private void CreatExplosion(RaycastHit raycastHit)
    {
        var vfx = Instantiate(explosion);
        vfx.transform.position = raycastHit.point;
        vfx.transform.localScale = Vector3.one * hitRange;
    }

    private void ApplyForceToPieces(List<MeshDestroy.PartMeshData> pieces, Vector3 hitpoint)
    {
        foreach (var piece in pieces)
        {
            var rb = piece.gameObject.GetComponent<Rigidbody>();
            if (!rb.isKinematic) rb.AddForce((piece.MeshWorldCenter - hitpoint).normalized * hitPower, ForceMode.Impulse);
        }
    }
}
