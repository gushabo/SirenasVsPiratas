// BuildMeta.cs
using UnityEngine;

public enum BuildKind { Tower, Mine, Upgrade } // Upgrade lo dispararemos desde el colocador

public class BuildMeta : MonoBehaviour
{
    public BuildKind kind = BuildKind.Tower;
}