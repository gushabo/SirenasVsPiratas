using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    [Header("Aqui va pregfabsowowo")]
    public List<GameObject> deck = new List<GameObject>();

    private int currentIndex = 0;

    public bool CanDraw => deck != null && deck.Count > 0;

    private void Start()
    {
        HandManager hand = FindFirstObjectByType<HandManager>();
        for (int i = 0; i < 3; i++)
            DrawCard(hand);
    }

    public void DrawCard(HandManager handManager)
    {
        if (!CanDraw || handManager == null) return;

        var prefab = deck[currentIndex];
        currentIndex = (currentIndex + 1) % deck.Count;
        handManager.AddCardToHand(prefab);
    }

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
            int randomIndex = Random.Range(0, deck.Count);
            var prefab = deck[randomIndex];
            handManager.AddCardToHand(prefab);
        }
    }

    // -------- NUEVO: utilidades para el draft --------
    public GameObject GetRandomCard()
    {
        if (!CanDraw) return null;
        int idx = Random.Range(0, deck.Count);
        return deck[idx];
    }

    /// Retorna una muestra aleatoria de 'count' cartas.
    /// allowDuplicates = false -> sin repetidos (si deck.Count < count, rellena con repetidos).
    public List<GameObject> GetRandomSample(int count, bool allowDuplicates = false)
    {
        var result = new List<GameObject>(count);
        if (!CanDraw) return result;

        if (allowDuplicates || deck.Count <= 1)
        {
            for (int i = 0; i < count; i++)
                result.Add(GetRandomCard());
            return result;
        }

        // Sin repetidos (hasta donde alcance)
        var indices = new List<int>(deck.Count);
        for (int i = 0; i < deck.Count; i++) indices.Add(i);

        // Fisher-Yates shuffle parcial
        for (int i = 0; i < Mathf.Min(count, deck.Count); i++)
        {
            int r = Random.Range(i, indices.Count);
            (indices[i], indices[r]) = (indices[r], indices[i]);
            result.Add(deck[indices[i]]);
        }

        // Si pidieron más que el tamaño del deck, rellena con aleatorias (permitiendo repetidos)
        while (result.Count < count)
            result.Add(GetRandomCard());

        return result;
    }
}
