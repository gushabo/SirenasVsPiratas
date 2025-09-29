using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    [Header("Aqui va pregfabsowowo")]
    public List<GameObject> deck = new List<GameObject>();

    private int currentIndex = 0;

  
    /// ¿Hay cartas para robar?
   
    public bool CanDraw => deck != null && deck.Count > 0;
    private void Start()
    {
        HandManager hand = FindFirstObjectByType<HandManager>();
        for(int i=0; i<6; i++)
        {
            DrawCard(hand);

        }
    }

   
    /// Entrega la siguiente carta del deck y la agrega a la mano.
  
    public void DrawCard(HandManager handManager)
    {
        if (!CanDraw || handManager == null) return;

        var prefab = deck[currentIndex];
        currentIndex = (currentIndex + 1) % deck.Count; // recorre circularmente

        handManager.AddCardToHand(prefab);
    }

   
    /// Agrega varias cartas seguidas (opcional).
    
    public void DrawCards(HandManager handManager, int count)
    {
        if (!CanDraw || handManager == null) return;
        for (int i = 0; i < count; i++)
            DrawCard(handManager);
    }

    public void DrawRandomCards(HandManager handManager, int count = 3)
    {
        if (!CanDraw || handManager == null) return;

        for (int i = 0; i < count; i++)
        {
            // escoger un índice aleatorio dentro del mazo
            int randomIndex = Random.Range(0, deck.Count);

            var prefab = deck[randomIndex];
            handManager.AddCardToHand(prefab);
        }
    }
}
