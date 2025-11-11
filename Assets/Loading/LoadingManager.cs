using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class LoadingManager : MonoBehaviour
{
    public Image barraProgreso;

    public static string EscenaObjetivo; // 👈 cambia a public static

    void Start()
    {
        StartCoroutine(CargarAsync());
    }

    IEnumerator CargarAsync()
    {
        yield return null;

        // Carga la escena objetivo que guardó SceneTransition
        AsyncOperation operacion = SceneManager.LoadSceneAsync(EscenaObjetivo);
        operacion.allowSceneActivation = false;

        while (!operacion.isDone)
        {
            float progreso = Mathf.Clamp01(operacion.progress / 0.9f);
            if (barraProgreso != null)
                barraProgreso.fillAmount = progreso;

            if (operacion.progress >= 0.9f)
            {
                yield return new WaitForSeconds(0.3f);
                operacion.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}
