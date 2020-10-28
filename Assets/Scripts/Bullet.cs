using UnityEngine;
using System.Collections.Generic;

public class Bullet : MonoBehaviour
{
    public float hitPower;
    public float hitRange;
    public float movementSpeed;
    public float spawnDistance;
    public float spawnHeight;
    public GameObject explosion;

    private float time;
    private Vector3 direction;
    
    public void Init(RaycastHit hitRay)
    {
        transform.position = new Vector3(hitRay.point.x, spawnHeight, hitRay.point.z - spawnDistance);
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
            var pieces = new List<MeshDestroy.PartMeshData>();

            var colliders = Physics.OverlapSphere(raycastHit.point, hitRange);
            foreach (var coll in colliders)
            {
                if (!coll.transform.CompareTag("Environment") && coll.gameObject != raycastHit.transform.gameObject)
                {
                    var breakVector = coll.transform.position - raycastHit.transform.position;
                    var meshes = MeshDestroy.BreakGameObject(coll.transform.gameObject, raycastHit.point, breakVector);
                    foreach (var mesh in meshes) pieces.Add(mesh);
                    pieces.Add(MeshDestroy.CreatePartMeshData(coll.gameObject));
                }
            }
            
            if (!raycastHit.transform.CompareTag("Environment"))
            {
                var breakVectorNorm = (raycastHit.point - Camera.main.ScreenToWorldPoint(Input.mousePosition)).normalized;
                var breakVector = breakVectorNorm * hitRange;
                var meshes = MeshDestroy.BreakGameObject(raycastHit.transform.gameObject, raycastHit.point, breakVector);
                foreach (var mesh in meshes) pieces.Add(mesh);
                pieces.Add(MeshDestroy.CreatePartMeshData(raycastHit.transform.gameObject));
            }
            
            ApplyForceToPieces(pieces, raycastHit.point);
        }
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
            if (!rb.isKinematic) rb.velocity += (piece.MeshWorldCenter - hitpoint).normalized * hitPower;
        }
        
        Destroy(gameObject);
    }
}
