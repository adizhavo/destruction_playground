using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HitRay : MonoBehaviour
{
    public float hitPower;
    public float hitPenetration;

    public GameObject BulletIndicator;
    public GameObject Bullet;

    public void Reset()
    {
        SceneManager.LoadScene(0);
    }

    void Update()
    {
        if (Input.GetMouseButtonUp(0))
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hitRay))
            {
                SpawnBullet(hitRay);
                
//                var direction = (hitRay.point - Camera.main.ScreenToWorldPoint(Input.mousePosition)).normalized;
//                var hitDirection = direction * hitPenetration;
//
//                var colliders = Physics.OverlapSphere(hitRay.point, hitPenetration);
//                var pieces = new List<MeshDestroy.PartMesh>();
//                foreach (var collider in colliders)
//                {
//                    if (!collider.transform.CompareTag("Environment") && collider.gameObject != hitRay.transform.gameObject)
//                        pieces.AddRange(MeshDestroy.BreakGameObject(collider.transform.gameObject, hitRay.point, collider.transform.position - hitRay.transform.position, false));
//                }
//                
//                if (!hitRay.transform.CompareTag("Environment"))
//                    pieces.AddRange(MeshDestroy.BreakGameObject(hitRay.transform.gameObject, hitRay.point, hitDirection));
//                
//                foreach (var piece in pieces)
//                {
//                    var rb = piece.gameObject.GetComponent<Rigidbody>();
//                    rb.AddExplosionForce(hitPower, hitRay.point, hitPenetration);
//                }
            }
        }
    }

    private void SpawnBullet(RaycastHit hitRay)
    {
        var indicator = GameObject.Instantiate(BulletIndicator);
        indicator.transform.position = hitRay.point;

        var bullet = GameObject.Instantiate(Bullet);
        bullet.GetComponent<Bullet>().Init(hitRay);
    }
}
