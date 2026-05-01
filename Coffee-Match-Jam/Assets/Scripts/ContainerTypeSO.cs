using UnityEngine;

public enum ContainerSize { Small = 4, Large = 6 }

[CreateAssetMenu(fileName = "ContainerType", menuName = "Coffee Match/Container Type")]
public class ContainerTypeSO : ScriptableObject
{
    public ContainerSize size;
    public int columns = 2;
    public int rows;        // 2 for Small, 3 for Large

    public int Capacity => (int)size;
}
