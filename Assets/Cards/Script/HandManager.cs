using UnityEngine;
using System.Collections.Generic;

public class HandManager : MonoBehaviour
{
    [Header("Prefabs/UI")]
    public GameObject cardPrefab;              // (opcional) no se usa en este setup, lo dejo para no romper nada
    public Transform handTransform;

    [Header("Cartas a repartir (arrástralas en el Inspector)")]
    public List<GameObject> buildCardPrefabs;  // 3 prefabs de cartas de TORRETA
    public List<GameObject> upgradeCardPrefabs; // 3 prefabs de cartas de MEJORA

    [Header("Layout")]
    public float fanSpread = 6f;
    public float cardSpacing = -120f;
    public float verticalSpacing = 40f;

    public List<GameObject> cardsInHand = new List<GameObject>();

    void Start()
    {
        // Reparte exactamente 3 y 3 (si faltan, solo pondrá las que existan)
        AddFirstN(buildCardPrefabs, 3);
        AddFirstN(upgradeCardPrefabs, 3);

        UpdateHandVisuals();
    }

    // --- Helpers ---
    void AddFirstN(List<GameObject> source, int n)
    {
        if (source == null) return;
        int count = Mathf.Min(n, source.Count);
        for (int i = 0; i < count; i++)
            AddCardToHand(source[i]);
    }

    // Mantengo tu método base, pero ahora recibe el prefab específico
    void AddCardToHand(GameObject cardPrefabToUse)
    {
        if (cardPrefabToUse == null) return;
        GameObject newCard = Instantiate(cardPrefabToUse, handTransform.position, Quaternion.identity, handTransform);
        cardsInHand.Add(newCard);
    }

    void Update()
    {
        UpdateHandVisuals();
    }

    void UpdateHandVisuals()
    {
        int cardCount = cardsInHand.Count;
        if (cardCount == 0) return;

        if (cardCount == 1)
        {
            cardsInHand[0].transform.localRotation = Quaternion.identity;
            cardsInHand[0].transform.localPosition = Vector3.zero;
            return;
        }

        for (int i = 0; i < cardCount; i++)
        {
            float rotationAngle = fanSpread * (i - (cardCount - 1) / 2f);
            Transform t = cardsInHand[i].transform;
            t.localRotation = Quaternion.Euler(0f, 0f, rotationAngle);

            float horizontalOffSet = cardSpacing * (i - (cardCount - 1) / 2f);
            float normalized = (2f * i / (cardCount - 1) - 1f);
            float verticalOffSet = verticalSpacing * (1 - normalized * normalized);

            t.localPosition = new Vector3(horizontalOffSet, verticalOffSet, 0f);
        }
    }
}
