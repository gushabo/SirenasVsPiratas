using UnityEngine;

public class ResetTutorialButton : MonoBehaviour
{
    // Si quieres también limpiar la flag de "primera torre colocada"
    [Header("Opcional")]
    public bool alsoClearFirstTowerPlaced = true;
    public string firstTowerKey = "first_tower_placed";

    // Llama esto desde el botón OnClick
    public void OnClickResetTutorial()
    {
        TutorialProgress.ResetAll();

        if (alsoClearFirstTowerPlaced)
            PlayerPrefs.DeleteKey(firstTowerKey);
<<<<<<< HEAD
            PlayerPrefs.DeleteKey(TutorialProgress.PREF_TUTORIAL_STEP);
           
            PlayerPrefs.Save();
       
=======
>>>>>>> parent of f49a8f1 (cambiosnormales)

        PlayerPrefs.Save();
        Debug.Log("[ResetTutorialButton] Tutorial reset. Se limpiaron PlayerPrefs del tutorial.");
        SceneTransition.CargarConPantallaNegra("Enemy");


    }
}