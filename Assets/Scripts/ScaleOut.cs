using System.Collections;
using UnityEngine;

public class ScaleOut : MonoBehaviour
{
    public float waitTime = 8;
    public float fadeTime = 0.5f;
    public bool destroy = true;
    
    private IEnumerator Start()
    {
        var scale = transform.localScale;
        yield return new WaitForSeconds(waitTime);

        var time = 0f;
        while (time < fadeTime)
        {
            time += Time.deltaTime;
            var ratio = time / fadeTime;
            transform.localScale = scale + (scale * ratio);
            yield return new WaitForEndOfFrame();
        }
        if (destroy)
        Destroy(gameObject);
    }
}

