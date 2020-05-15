using System.Collections;
using UnityEngine;

public class ClearGameObject : MonoBehaviour
{
    public float waitTime = 8;
    public float fadeTime = 0.5f;

    public void SetWaiTime(float waitTime)
    {
        this.waitTime = waitTime;
    }
    
    private IEnumerator Start()
    {
        var meshRender = gameObject.GetComponent<MeshRenderer>();
        yield return new WaitForSeconds(waitTime);
        meshRender.castShadows = false;

        var time = 0f;
        while (time < fadeTime)
        {
            time += Time.deltaTime;
            var ratio = time / fadeTime;
            meshRender.material.SetFloat("_Transparency", ratio);
            yield return new WaitForEndOfFrame();
        }
        Destroy(gameObject);
    }
}
