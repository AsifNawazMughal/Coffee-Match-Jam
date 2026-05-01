using System.Collections;
using UnityEngine;

// Sits on each Generator GameObject. Feeds containers into its LaneController.
public class ContainerGeneratorCtrl : MonoBehaviour
{
    public LaneController targetLane;
    public float checkInterval = 2.5f;

    private GameManager gm;

    public void Initialize(GameManager manager)
    {
        gm = manager;
        StartCoroutine(Loop());
    }

    IEnumerator Loop()
    {
        // Small initial delay so scene is fully set up
        yield return new WaitForSeconds(1f);

        while (true)
        {
            yield return new WaitForSeconds(checkInterval);
            if (targetLane != null && targetLane.CanAcceptNew)
                Spawn();
        }
    }

    void Spawn()
    {
        PackageColor color      = gm.GetRandomColor();
        var (prefab, typeSO)    = gm.GetRandomContainerSpec();
        if (prefab == null) return;

        var go        = Instantiate(prefab, transform.position, Quaternion.identity);
        var container = go.GetComponent<Container>();
        if (container == null) { Destroy(go); return; }

        var colorDef = gm.GetColorDef(color);
        container.Init(color, typeSO, colorDef?.material);
        targetLane.Enqueue(container);
    }
}
