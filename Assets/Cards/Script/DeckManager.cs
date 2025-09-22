using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    [Header("Deck (arrastra prefabs aquí en el Inspector)")]
    public List<GameObject> deck = new List<GameObject>();

    private int currentIndex = 0;

    /// <summary>
    /// ¿Hay cartas para robar?
    /// </summary>
    public bool CanDraw => deck != null && deck.Count > 0;
    private void Start()
    {
        HandManager hand = FindFirstObjectByType<HandManager>();
        for(int i=0; i<6; i++)
        {
            DrawCard(hand);

        }
    }
    /// <summary>
    /// Entrega la siguiente carta del deck y la agrega a la mano.
    /// </summary>
    public void DrawCard(HandManager handManager)
    {
        if (!CanDraw || handManager == null) return;

        var prefab = deck[currentIndex];
        currentIndex = (currentIndex + 1) % deck.Count; // recorre circularmente

        handManager.AddCardToHand(prefab);
    }

    /// <summary>
    /// Agrega varias cartas seguidas (opcional).
    /// </summary>
    public void DrawCards(HandManager handManager, int count)
    {
        if (!CanDraw || handManager == null) return;
        for (int i = 0; i < count; i++)
            DrawCard(handManager);
    }
}
