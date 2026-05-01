using UnityEngine;

[CreateAssetMenu(fileName = "ColorDef", menuName = "Coffee Match/Color Definition")]
public class ColorDefinitionSO : ScriptableObject
{
    public PackageColor colorType;
    public Color displayColor;
    public Material material;
}
