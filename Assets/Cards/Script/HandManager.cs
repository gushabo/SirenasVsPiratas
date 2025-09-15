using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System;

public class HandManager : MonoBehaviour
{


    public GameObject cardPrefab; // card prefab
    public Transform handTransform;



    public float fanSpread = 6f;

    public float cardSpacing = -120f;


    public float verticalSpacing = 40f;

    public List<GameObject> cardsInHand = new List<GameObject>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        AddCardToHand();
        AddCardToHand();
        AddCardToHand();
        AddCardToHand();
        AddCardToHand();
        AddCardToHand();
    }

    private void AddCardToHand()
    {
        GameObject newCard = Instantiate(cardPrefab, handTransform.position, Quaternion.identity, handTransform);
        cardsInHand.Add(newCard);

        UpdateHandVisuals();
    }


    private void Update()
    {
        UpdateHandVisuals();
    }

    private void UpdateHandVisuals()
    {

        int cardCount = cardsInHand.Count;

        if (cardCount == 1)
        {
            cardsInHand[0].transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            cardsInHand[0].transform.localPosition = new Vector3(0f, 0f, 0f);
            return;
        }

        for (int i = 0; i < cardCount; i++)
        {
            float rotationAngle = (fanSpread * (i - (cardCount - 1) / 2f));
            cardsInHand[i].transform.localRotation = Quaternion.Euler(0f, 0f, rotationAngle);


            float horizontalOffSet = (cardSpacing * (i - (cardCount - 1) / 2f));

            float NormalizedPosition = (2f * i / (cardCount - 1) - 1f);


            float verticalOffSet = verticalSpacing * (1 - NormalizedPosition * NormalizedPosition);

            //Eta cosa hace la uibicacion
            cardsInHand[i].transform.localPosition = new Vector3(horizontalOffSet, verticalOffSet, 0f);
        }

    }
}
