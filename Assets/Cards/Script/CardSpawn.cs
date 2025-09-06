using UnityEngine;
using UnityEngine.EventSystems;

public class CardSpawn : MonoBehaviour
{
    public GameObject worldPrefab;

    public void OnCardClicked()
    {
        CardPlacer.Instance.SetSelectedCard(worldPrefab);
        Debug.Log("Clickeado");
    }
}
