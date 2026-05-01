using System.Collections;
using UnityEngine;

// Sits on each Generator GameObject.
// Periodically creates a container and pushes it into the target lane.
public class ContainerGeneratorCtrl : MonoBehaviour
{
    public LaneQueue targetLane;
    public float checkInterval = 2f;

    private GameManager gm;

    public void Initialize(GameManager manager)
    {
        gm = manager;
        StartCoroutine(Loop());
    }

    IEnumerator Loop()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkInterval);
            if (targetLane != null && targetLane.CanAcceptNew)
                Spawn();
        }
    }

    void Spawn()
    {
        PackageColor color       = gm.GetRandomColor();
        var (prefab, typeSO)     = gm.GetRandomContainerSpec();
        if (prefab == null) return;

        var go        = Instantiate(prefab, transform.position, Quaternion.identity);
        var container = go.GetComponent<Container>();
        if (container == null) { Destroy(go); return; }

        var colorDef = gm.GetColorDef(color);
        container.Init(color, typeSO, colorDef?.material);
        targetLane.Enqueue(container);
    }
}
