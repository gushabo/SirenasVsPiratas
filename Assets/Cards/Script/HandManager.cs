using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System;

public class HandManager : MonoBehaviour
{


    public GameObject cardPrefab; // card prefab
    public Transform handTransform;

    public float fanSpread = 5f;

    public List<GameObject> cardsInHand = new List<GameObject>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        AddCardToHand();
    }

    private void AddCardToHand()
    {
        GameObject newCard = Instantiate(cardPrefab, handTransform.position, Quaternion.identity, handTransform);
        cardsInHand.Add(newCard);

        UpdateHandVisuals();
    }

    private void UpdateHandVisuals()
    {
        


    }
}
