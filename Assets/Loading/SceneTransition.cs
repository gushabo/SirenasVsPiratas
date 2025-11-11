using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class SceneTransition : MonoBehaviour
{
    private static CanvasGroup overlayCanvas;

    public static void CargarConPantallaNegra(string escenaObjetivo)
    {
        if (overlayCanvas == null)
        {
            // Crea Canvas
            GameObject canvasGO = new GameObject("TransitionCanvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;

            overlayCanvas = canvasGO.AddComponent<CanvasGroup>();

            // Crea fondo negro
            GameObject fondoGO = new GameObject("FondoNegro");
            fondoGO.transform.SetParent(canvasGO.transform, false);
            Image fondo = fondoGO.AddComponent<Image>();
            fondo.color = Color.black;
            RectTransform rt = fondo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        overlayCanvas.alpha = 1; // aparece pantalla negra instantánea

        // 👇 antes de cambiar de escena, guarda el nombre en el LoadingManager
        LoadingManager.EscenaObjetivo = escenaObjetivo;

        // 👇 luego carga la escena de loading
        GameObject runner = new GameObject("TransitionRunner");
        runner.AddComponent<SceneTransition>().StartCoroutine(CargarAsync(runner));
    }

    private static IEnumerator CargarAsync(GameObject runner)
    {
        yield return null;
        SceneManager.LoadScene("LoadingScene");
        GameObject.Destroy(runner);
    }
}
