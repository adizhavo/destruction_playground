using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        time += Time.deltaTime;
        if (time > 20)
        {
            Destroy(gameObject);
            return;
        }
        
        var previous = transform.position;
        transform.Translate(direction * Time.deltaTime * movementSpeed);
        var next = transform.position;

        if (Physics.Linecast(previous, next, out var raycastHit))
        {
            var vfx = Instantiate(explosion);
            vfx.transform.position = raycastHit.point;
            vfx.transform.localScale = Vector3.one * hitRange;
            
            var direction = (raycastHit.point - Camera.main.ScreenToWorldPoint(Input.mousePosition)).normalized;
            var hitDirection = direction * hitRange;

            var colliders = Physics.OverlapSphere(raycastHit.point, hitRange);
            var pieces = new List<GameObject>();
            foreach (var collider in colliders)
            {
                if (!collider.transform.CompareTag("Environment") &&
                    collider.gameObject != raycastHit.transform.gameObject)
                {
                    pieces.Add(collider.gameObject);
                    if (collider.GetComponent<Rigidbody>().mass > MeshDestroy.minMassDestroy)
                    {
                        MeshDestroy.BreakGameObject(collider.transform.gameObject, raycastHit.point, collider.transform.position - raycastHit.transform.position);
                    }
                }
            }

            if (!raycastHit.transform.CompareTag("Environment"))
            {
                pieces.Add(raycastHit.transform.gameObject);
                if (raycastHit.transform.GetComponent<Rigidbody>().mass > MeshDestroy.minMassDestroy)
                {
                    MeshDestroy.BreakGameObject(raycastHit.transform.gameObject, raycastHit.point, hitDirection);
                }
            }

            foreach (var piece in pieces)
            {
                var rb = piece.gameObject.GetComponent<Rigidbody>();
                rb.velocity += (piece.transform.position - raycastHit.point).normalized * hitPower;
            }
            
            Destroy(gameObject);
        }
    }
}
