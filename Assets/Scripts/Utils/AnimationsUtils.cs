using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationsUtils : MonoBehaviour
{
    public IEnumerator ScaleOverTime(GameObject obj, Vector3 toScale, float duration) {

        obj.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

        float counter = 0;

        Vector3 startScaleSize = obj.transform.localScale;

        while (counter < duration) {

            counter += Time.deltaTime;

            obj.transform.localScale = Vector3.Lerp(startScaleSize, toScale, counter / duration);

            yield return null;
        }
    }

    public IEnumerator ScaleDownOverTime(GameObject obj, Vector3 toScale, float duration) {

        Vector3 startScaleSize = obj.transform.localScale;

        float counter = 0;

        while (counter < duration) {

            counter += Time.deltaTime;

            obj.transform.localScale = Vector3.Lerp(startScaleSize, toScale, counter / duration);

            yield return null;
        }

        obj.transform.localScale = toScale;
    }
}
