using UnityEngine;
using UnityEngine.SceneManagement;

public class HitRay : MonoBehaviour
{
    public GameObject BulletIndicator;
    public GameObject Bullet;

    public void Reset()
    {
        SceneManager.LoadScene(0);
    }

    void Update()
    {
        if (Input.GetMouseButtonUp(0) && !CameraDrag.dragging)
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hitRay))
            {
                SpawnBullet(hitRay);
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
