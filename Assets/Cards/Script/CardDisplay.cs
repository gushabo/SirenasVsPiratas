using UnityEngine;
using UnityEngine.UI;
using TMPro;



public class CardDisplay : MonoBehaviour
{

   

    public EnemyTypes Enemy;
    public Image cardImage;
    public Image[] typeImages;
    public TMP_Text DamageText;
    //public TMP_Text NameText;
    public TMP_Text GoldText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        UpdateCardDisplay();
    }

    public void UpdateCardDisplay()
    {
       
        cardImage.sprite = Enemy.image;

    }
}
